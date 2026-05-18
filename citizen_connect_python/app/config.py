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
    itop_organization_id: str = "1"
    itop_caller_id: str = "1"
    itop_service_id: str = "1"
    itop_service_subcategory_id: str = "16"
    file_storage_base_url: str = "http://localhost:8000/uploads"

    sms_provider_base_url: str = "http://msg.msgclub.net/rest/services/sendSMS/sendGroupSms"
    sms_provider_auth_key: str = "d269accd9fdd36824632fc1eccabeb"
    sms_provider_sender_id: str = "MLSMPK"
    sms_provider_route_id: str = "8"
    sms_provider_sms_content_type: str = "english"

    otp_expiry_minutes: int = 5
    otp_max_verify_attempts: int = 5
    otp_resend_cooldown_seconds: int = 60
    otp_max_resends_per_day: int = 25
    otp_fixed_users: str = "9900139826:123456:MLA,8951908951:654321:Citizen"


settings = Settings()
