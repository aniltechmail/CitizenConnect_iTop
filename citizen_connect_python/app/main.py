import asyncio
from contextlib import suppress
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pathlib import Path
from app.config import settings
from app.database import AsyncSessionFactory
from app.routers import auth, location, complaint, notification, escalation, dashboard, report
from app.services.escalation_service import EscalationService

app = FastAPI(
    title=settings.app_name,
    version=settings.app_version,
    docs_url="/swagger",
    redoc_url="/redoc"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Serve uploaded files statically
uploads_path = Path("uploads")
uploads_path.mkdir(exist_ok=True)
app.mount("/uploads", StaticFiles(directory="uploads"), name="uploads")

app.include_router(auth.router)
app.include_router(location.router)
app.include_router(complaint.router)
app.include_router(notification.router)
app.include_router(escalation.router)
app.include_router(dashboard.router)
app.include_router(report.router)


async def run_escalation_scanner() -> None:
    while True:
        try:
            async with AsyncSessionFactory() as session:
                service = EscalationService(session)
                await service.scan()
                await session.commit()
        except Exception as exc:
            print(f"[SLA] Escalation scanner failed: {exc}")
        await asyncio.sleep(30 * 60)


@app.on_event("startup")
async def start_escalation_scanner() -> None:
    app.state.escalation_task = asyncio.create_task(run_escalation_scanner())


@app.on_event("shutdown")
async def stop_escalation_scanner() -> None:
    task = getattr(app.state, "escalation_task", None)
    if task:
        task.cancel()
        with suppress(asyncio.CancelledError):
            await task


@app.get("/", tags=["Health"])
async def root():
    return {
        "status": "ok",
        "app": settings.app_name,
        "version": settings.app_version
    }
