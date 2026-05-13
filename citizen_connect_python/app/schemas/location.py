from pydantic import BaseModel


class DistrictSchema(BaseModel):
    id: int
    name: str
    code: str

    model_config = {"from_attributes": True}


class ConstituencySchema(BaseModel):
    id: int
    name: str
    code: str
    district_id: int

    model_config = {"from_attributes": True}


class AreaSchema(BaseModel):
    id: int
    name: str
    code: str
    constituency_id: int

    model_config = {"from_attributes": True}


class BlockSchema(BaseModel):
    id: int
    name: str
    code: str
    area_id: int

    model_config = {"from_attributes": True}