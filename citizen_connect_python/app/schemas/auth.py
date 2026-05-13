from pydantic import AliasChoices, BaseModel, EmailStr, Field, field_validator
import re


class CitizenRegisterSchema(BaseModel):
    model_config = {"populate_by_name": True}

    full_name: str = Field(
        validation_alias=AliasChoices("full_name", "fullName", "FullName")
    )
    phone: str
    email: EmailStr | None = None
    password: str
    block_id: int = Field(
        validation_alias=AliasChoices("block_id", "blockId", "BlockId")
    )

    @field_validator("phone")
    @classmethod
    def validate_phone(cls, v: str) -> str:
        if not re.match(r"^\+?[0-9]{10,15}$", v):
            raise ValueError("Invalid phone number format")
        return v

    @field_validator("password")
    @classmethod
    def validate_password(cls, v: str) -> str:
        if len(v) < 6:
            raise ValueError("Password must be at least 6 characters")
        return v


class LoginSchema(BaseModel):
    identifier: str   # phone or email
    password: str


class UserInfoSchema(BaseModel):
    id: str
    full_name: str
    phone: str | None = None
    email: str | None = None
    user_type: str        # "citizen" or "internal"
    role: str | None = None

    model_config = {"from_attributes": True}


class AuthResponseSchema(BaseModel):
    token: str
    refresh_token: str
    expires_at: str
    user: UserInfoSchema
