from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pathlib import Path
from app.config import settings
from app.routers import auth, location, complaint

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


@app.get("/", tags=["Health"])
async def root():
    return {
        "status": "ok",
        "app": settings.app_name,
        "version": settings.app_version
    }