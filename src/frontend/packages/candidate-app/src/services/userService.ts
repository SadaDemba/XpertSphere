import { BaseClient } from './BaseClient';
import { ResponseResult } from 'src/models';
import type { User } from '../models/auth';
import type { Currency } from '../enums';

export interface UpdateUserSkillsDto {
  skills?: string;
}

export interface UpdateUserProfileDto {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;

  // Address Information
  streetNumber?: string;
  street?: string;
  city?: string;
  postalCode?: string;
  region?: string;
  country?: string;
  addressLine2?: string;

  // Professional Information
  yearsOfExperience?: number | undefined;
  desiredSalary?: number | undefined;
  desiredSalaryCurrency?: Currency | undefined;
  availability?: string;
  linkedInProfile?: string;
}

export interface UploadCvDto {
  cvFile: File;
}

export interface UploadCvResponseDto {
  success: boolean;
  message: string;
  fileName?: string;
  fileSize?: number;
  uploadDate?: string;
}

export class UserService extends BaseClient {
  constructor() {
    super('/Users');
  }

  async updateUserSkills(
    userId: string,
    skillsDto: UpdateUserSkillsDto,
  ): Promise<ResponseResult<User> | null> {
    return this.put<ResponseResult<User>>(
      `/${userId}/skills`,
      skillsDto,
      'Erreur lors de la mise à jour des compétences',
    );
  }

  async updateUserProfile(
    userId: string,
    profileDto: UpdateUserProfileDto,
  ): Promise<ResponseResult<User> | null> {
    return this.put<ResponseResult<User>>(
      `/${userId}/profile`,
      profileDto,
      'Erreur lors de la mise à jour du profil',
    );
  }

  async uploadCv(
    userId: string,
    cvFile: File,
  ): Promise<ResponseResult<UploadCvResponseDto> | null> {
    const formData = new FormData();
    formData.append('cvFile', cvFile);

    return this.postFormData<ResponseResult<UploadCvResponseDto>>(
      `/${userId}/cv`,
      formData,
      "Erreur lors de l'upload du CV",
    );
  }

  async downloadCv(userId: string): Promise<Blob> {
    return this.downloadFile(`/${userId}/cv`, {}, 'Erreur lors du téléchargement du CV');
  }
}

export const userService = new UserService();
