from datetime import datetime
from pydantic import AliasChoices, BaseModel, Field


class RefreshTokenRequestSchema(BaseModel):
    model_config = {"populate_by_name": True}
    refresh_token: str = Field(validation_alias=AliasChoices("refresh_token", "refreshToken", "RefreshToken"))


class SendOtpRequestSchema(BaseModel):
    model_config = {"populate_by_name": True}
    phone: str = Field(default="", validation_alias=AliasChoices("phone", "Phone", "mobileNumber", "MobileNumber", "mobile_number"))


class SendOtpResponseSchema(BaseModel):
    model_config = {"populate_by_name": True}
    phone: str
    mobile_number: str = Field(default="", serialization_alias="mobileNumber")
    expires_at: datetime | None = Field(default=None, serialization_alias="expiresAt")
    expires_on: datetime | None = Field(default=None, serialization_alias="expiresOn")
    dev_otp: str | None = Field(default=None, serialization_alias="devOtp")
    otp: str | None = None
    is_new_otp: bool = Field(default=False, serialization_alias="isNewOtp")
    message: str = ""


class VerifyOtpRequestSchema(BaseModel):
    model_config = {"populate_by_name": True}
    phone: str = Field(default="", validation_alias=AliasChoices("phone", "Phone", "mobileNumber", "MobileNumber", "mobile_number"))
    otp: str


class RegisterDeviceRequestSchema(BaseModel):
    model_config = {"populate_by_name": True}
    device_token: str = Field(validation_alias=AliasChoices("device_token", "deviceToken", "DeviceToken"))
    platform: str = "unknown"
