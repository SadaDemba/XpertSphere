export interface TrainingDto {
  id: string;
  userId: string;
  school: string;
  level: string;
  period: string;
  field: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateTrainingDto {
  userId: string;
  school: string;
  level: string;
  period: string;
  field: string;
}
