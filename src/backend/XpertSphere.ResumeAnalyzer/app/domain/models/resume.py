from dataclasses import dataclass, field
from typing import List, Optional
from uuid import uuid4


@dataclass
class Experience:
    """Professional experience"""

    title: str
    description: str
    date: Optional[str] = None
    company: Optional[str] = None
    location: Optional[str] = None

    def __post_init__(self):
        """Validate and clean data after initialization"""
        self.title = self.title.strip() if self.title else self.title
        self.description = (
            self.description.strip() if self.description else self.description
        )


@dataclass
class Training:
    """Educational training"""

    school: str
    level: str
    period: Optional[str] = None
    field: Optional[str] = None

    def __post_init__(self):
        """Validate and clean data after initialization"""
        self.school = self.school.strip() if self.school else self.school
        self.level = self.level.strip() if self.level else self.level


@dataclass
class CVModel:
    """Complete CV model"""

    first_name: str
    last_name: str
    email: Optional[str] = None
    phone_number: Optional[str] = None
    profession: Optional[str] = None
    address: Optional[str] = None
    languages: List[str] = field(default_factory=list)
    trainings: List[Training] = field(default_factory=list)
    skills: List[str] = field(default_factory=list)
    experiences: List[Experience] = field(default_factory=list)
    id: str = field(default_factory=lambda: str(uuid4()))

    def __post_init__(self):
        """Validate and clean data after initialization"""
        self.first_name = (
            self.first_name.strip() if self.first_name else self.first_name
        )
        self.last_name = self.last_name.strip() if self.last_name else self.last_name
        self.email = self.email.strip().lower() if self.email else self.email

    @property
    def full_name(self) -> str:
        """Get full name"""
        return f"{self.first_name} {self.last_name}".strip()
