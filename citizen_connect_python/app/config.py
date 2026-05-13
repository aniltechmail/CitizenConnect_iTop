from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False
    )

    database_url: str
    sync_database_url: str
    jwt_secret_key: str
    jwt_algorithm: str = "HS256"
    jwt_expiry_hours: int = 24
    app_name: str = "CitizenConnect API"
    app_version: str = "1.0.0"
    debug: bool = False

    # Individual DB fields for safe URL construction
    db_host: str = "localhost"
    db_port: int = 5432
    db_name: str = "CitizenConnect_API"
    db_user: str = "postgres"
    db_password: str = ""

    itop_enabled: bool = True
    itop_base_url: str = "http://localhost/itop"
    itop_api_version: str = "1.3"
    itop_username: str = "admin"
    itop_password: str = "T3rm!n@t0r1"
    itop_ticket_class: str = "UserRequest"
    itop_organization_id: str = ""
    itop_caller_id: str = ""
    itop_service_id: str = ""
    itop_service_subcategory_id: str = ""
    file_storage_base_url: str = "http://localhost:8000/uploads"


settings = Settings()
