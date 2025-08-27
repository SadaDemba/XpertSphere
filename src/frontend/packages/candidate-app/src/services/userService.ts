import { BaseClient } from './BaseClient';
import { ResponseResult } from 'src/models';
import type { User } from '../models/auth';

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
  availability?: string;
  linkedInProfile?: string;
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
}

export const userService = new UserService();
