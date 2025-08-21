import { BaseClient } from './BaseClient';
import { settings } from '../settings';
import type {
  LoginDto,
  AuthResponseDto,
  RegisterCandidateDto,
  ResumeAnalysisResponse,
  User,
} from '../models/auth';
import axios from 'axios';

export interface UserInfo extends User {
  name?: string;
  roles?: string[];
}

export class AuthService extends BaseClient {
  constructor() {
    super('/Auth');
  }

  async login(loginDto: LoginDto): Promise<AuthResponseDto | null> {
    return this.post<AuthResponseDto>('/login', loginDto, 'Erreur lors de la connexion');
  }

  async registerCandidate(
    registerDto: RegisterCandidateDto,
    resume?: File,
  ): Promise<AuthResponseDto | null> {
    try {
      const formData = new FormData();

      // Add all form fields
      Object.entries(registerDto).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          if (key === 'trainings' || key === 'experiences') {
            // Convert arrays to JSON strings
            formData.append(key, JSON.stringify(value));
          } else {
            formData.append(key, String(value));
          }
        }
      });

      // Add resume file if provided
      if (resume) {
        formData.append('resume', resume);
      }

      const response = await this.apiClient.post<AuthResponseDto>('/register/candidate', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      return response.data;
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : "Erreur lors de l'inscription");
    }
  }

  async analyzeResume(resume: File): Promise<ResumeAnalysisResponse | null> {
    const formData = new FormData();
    formData.append('file', resume);

    try {
      const response = await axios.post<ResumeAnalysisResponse>(
        `${settings.resumeAnalyzer.baseUrl}/extract/?include_raw_text=false`,
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        },
      );
      return response.data;
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : "Erreur lors de l'analyse du CV");
    }
  }

  /**
   * Get current user information
   */
  public async getCurrentUser(): Promise<User | null> {
    try {
      const response = await this.get<User>('/me');
      if (response) {
        // Add compatibility fields
        const userInfo: UserInfo = {
          ...response,
          name: `${response.firstName} ${response.lastName}`,
          roles: response.roles ?? [],
        };
        return userInfo;
      }
      return null;
    } catch (error) {
      console.error('Failed to get current user:', error);
      return null;
    }
  }
}

export const authService = new AuthService();
