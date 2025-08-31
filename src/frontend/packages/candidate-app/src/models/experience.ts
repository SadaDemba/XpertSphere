export interface ExperienceDto {
  id: string;
  userId: string;
  title: string;
  description: string;
  location: string;
  company: string;
  date: string;
  isCurrent: boolean;
  createdAt: string;
  updatedAt?: string;
  userFullName?: string;
  userEmail?: string;
}

export interface CreateExperienceDto {
  userId: string;
  title: string;
  description?: string;
  location: string;
  company: string;
  date: string;
  isCurrent: boolean;
}
