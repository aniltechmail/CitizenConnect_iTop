from datetime import datetime
from sqlalchemy import Integer, String, Boolean, DateTime, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.database import Base


class District(Base):
    __tablename__ = "districts"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(200), nullable=False)
    code: Mapped[str] = mapped_column("Code", String(20), nullable=False, unique=True)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))

    constituencies: Mapped[list["Constituency"]] = relationship(
        back_populates="district"
    )


class Constituency(Base):
    __tablename__ = "constituencies"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(200), nullable=False)
    code: Mapped[str] = mapped_column("Code", String(20), nullable=False, unique=True)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))
    district_id: Mapped[int] = mapped_column(
        "DistrictId", Integer, ForeignKey("districts.Id"), nullable=False
    )

    district: Mapped["District"] = relationship(back_populates="constituencies")
    areas: Mapped[list["Area"]] = relationship(back_populates="constituency")


class Area(Base):
    __tablename__ = "areas"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(200), nullable=False)
    code: Mapped[str] = mapped_column("Code", String(20), nullable=False, unique=True)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))
    constituency_id: Mapped[int] = mapped_column(
        "ConstituencyId", Integer, ForeignKey("constituencies.Id"), nullable=False
    )

    constituency: Mapped["Constituency"] = relationship(back_populates="areas")
    blocks: Mapped[list["Block"]] = relationship(back_populates="area")


class Block(Base):
    __tablename__ = "blocks"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(200), nullable=False)
    code: Mapped[str] = mapped_column("Code", String(20), nullable=False, unique=True)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))
    area_id: Mapped[int] = mapped_column(
        "AreaId", Integer, ForeignKey("areas.Id"), nullable=False
    )

    area: Mapped["Area"] = relationship(back_populates="blocks")
    citizens: Mapped[list["Citizen"]] = relationship(back_populates="block")