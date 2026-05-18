import asyncio
from contextlib import suppress
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from fastapi.staticfiles import StaticFiles
from pathlib import Path
from app.config import settings
from app.database import AsyncSessionFactory
from app.routers import auth, location, complaint, notification, escalation, dashboard, report, master, admin, mobile_auth, device, mobile_complaint, sync
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


@app.middleware("http")
async def api_version_alias(request, call_next):
    if request.scope["path"].startswith("/api/v1/"):
        request.scope["path"] = "/api/" + request.scope["path"][8:]
    return await call_next(request)

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
app.include_router(master.router)
app.include_router(admin.router)
app.include_router(mobile_auth.router)
app.include_router(device.router)
app.include_router(mobile_complaint.router)
app.include_router(sync.router)


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
    portal_index = Path(__file__).resolve().parents[2] / "web-portal" / "index.html"
    if portal_index.exists():
        return FileResponse(portal_index)
    return {"status": "ok", "app": settings.app_name, "version": settings.app_version}


portal_path = Path(__file__).resolve().parents[2] / "web-portal"
if portal_path.exists():
    app.mount("/assets", StaticFiles(directory=portal_path), name="portal-assets")


@app.get("/styles.css", include_in_schema=False)
async def portal_styles():
    return FileResponse(Path(__file__).resolve().parents[2] / "web-portal" / "styles.css")


@app.get("/app.js", include_in_schema=False)
async def portal_app():
    return FileResponse(Path(__file__).resolve().parents[2] / "web-portal" / "app.js")


@app.get("/config.js", include_in_schema=False)
async def portal_config():
    return FileResponse(Path(__file__).resolve().parents[2] / "web-portal" / "config.js")


@app.get("/{portal_path:path}", include_in_schema=False)
async def portal_fallback(portal_path: str):
    if portal_path.startswith(("api/", "swagger", "redoc", "uploads")):
        raise HTTPException(status_code=404)
    index = Path(__file__).resolve().parents[2] / "web-portal" / "index.html"
    return FileResponse(index)
