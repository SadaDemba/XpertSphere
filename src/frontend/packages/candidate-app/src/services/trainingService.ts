import type { CreateTrainingDto, TrainingDto } from 'src/models/training';
import { BaseClient } from './BaseClient';
import { ResponseResult } from 'src/models';

export class TrainingService extends BaseClient {
  constructor() {
    super('/Trainings');
  }

  async getUserTrainings(userId: string): Promise<ResponseResult<TrainingDto[]> | null> {
    return this.get<ResponseResult<TrainingDto[]>>(
      `/user/${userId}`,
      {},
      "Erreur lors de la récupération des formations de l'utilisateur",
    );
  }

  async replaceUserTrainings(
    userId: string,
    trainings: CreateTrainingDto[],
  ): Promise<ResponseResult<TrainingDto[]> | null> {
    return this.put<ResponseResult<TrainingDto[]>>(
      `/user/${userId}/replace`,
      trainings,
      'Erreur lors de la mise à jour des formations',
    );
  }
}

export const trainingService = new TrainingService();
