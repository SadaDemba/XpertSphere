import type { CreateExperienceDto, ExperienceDto } from 'src/models/experience';
import { BaseClient } from './BaseClient';
import { ResponseResult } from 'src/models';

export class ExperienceService extends BaseClient {
  constructor() {
    super('/Experiences');
  }

  async getExperienceById(id: string): Promise<ResponseResult<ExperienceDto> | null> {
    return this.get<ResponseResult<ExperienceDto>>(
      `/${id}`,
      {},
      "Erreur lors de la récupération de l'expérience",
    );
  }

  async getUserExperiences(userId: string): Promise<ResponseResult<ExperienceDto[]> | null> {
    return this.get<ResponseResult<ExperienceDto[]>>(
      `/user/${userId}`,
      {},
      "Erreur lors de la récupération des expériences de l'utilisateur",
    );
  }

  async replaceUserExperiences(
    userId: string,
    experiences: CreateExperienceDto[],
  ): Promise<ResponseResult<ExperienceDto[]> | null> {
    return this.put<ResponseResult<ExperienceDto[]>>(
      `/user/${userId}/replace`,
      experiences,
      'Erreur lors de la mise à jour des expériences',
    );
  }
}

export const experienceService = new ExperienceService();
