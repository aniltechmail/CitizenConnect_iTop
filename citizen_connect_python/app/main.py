from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from app.config import settings
from app.routers import auth, location

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

app.include_router(auth.router)
app.include_router(location.router)


@app.get("/", tags=["Health"])
async def root():
    return {"status": "ok", "app": settings.app_name, "version": settings.app_version}