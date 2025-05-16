from dataclasses import dataclass
from typing import List


@dataclass
class Experience:
    title: str
    description: str
    date: str


@dataclass
class Training:
    school: str
    level: str
    period: str


@dataclass
class UserModel:
    first_name: str
    last_name: str
    email: str
    phone_number: str
    profession: str
    address: str
    languages: List[str]
    trainings: List[Training]
    skills: List[str]
    experiences: List[Experience]
