import { BaseClient } from './BaseClient';
import { settings } from '../settings';
import type {
  LoginDto,
  RegisterCandidateDto,
  ResumeAnalysisResponse,
  User,
  AuthResult,
  Training,
  Experience,
} from '../models/auth';
import axios from 'axios';
import { ResponseResult } from 'src/models';
import type { ValidationProblemDetails } from '../models/base';

export interface UserInfo extends User {
  name?: string;
}

export class AuthService extends BaseClient {
  constructor() {
    super('/Auth');
  }

  async login(loginDto: LoginDto): Promise<AuthResult | null> {
    return this.post<AuthResult>('/login', loginDto, 'Erreur lors de la connexion');
  }

  async registerCandidate(
    registerDto: RegisterCandidateDto,
    resume?: File,
  ): Promise<AuthResult | ValidationProblemDetails | null> {
    try {
      const formData = new FormData();

      // Add all form fields
      Object.entries(registerDto).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          if (key === 'trainings' && Array.isArray(value)) {
            // Add trainings with indexed notation. Fields are appended even when
            // empty so the backend sees the real submitted value if the frontend
            // gating (MultiStepRegisterForm.vue, step "Formations") is ever bypassed.
            value.forEach((training: Training, index: number) => {
              formData.append(`trainings[${index}].school`, training.school ?? '');
              formData.append(`trainings[${index}].level`, training.level ?? '');
              formData.append(`trainings[${index}].period`, training.period ?? '');
              formData.append(`trainings[${index}].field`, training.field ?? '');
            });
          } else if (key === 'experiences' && Array.isArray(value)) {
            // Add experiences with indexed notation
            value.forEach((experience: Experience, index: number) => {
              if (experience.company)
                formData.append(`experiences[${index}].company`, experience.company);
              if (experience.location)
                formData.append(`experiences[${index}].location`, experience.location);
              if (experience.title)
                formData.append(`experiences[${index}].title`, experience.title);
              if (experience.date) formData.append(`experiences[${index}].date`, experience.date);
              // Always send description (even empty) so a bypass of the frontend
              // gate is still visible to, and rejected by, the backend.
              formData.append(`experiences[${index}].description`, experience.description ?? '');
            });
          } else {
            formData.append(key, String(value));
          }
        }
      });

      // Add resume file if provided
      if (resume) {
        formData.append('resume', resume);
      }

      const result = await this.postFormData<AuthResult | ValidationProblemDetails>(
        '/register/candidate',
        formData,
        "Erreur lors de l'inscription",
      );
      return result;
    } catch (error) {
      throw new Error(error instanceof Error ? error.message : "Erreur lors de l'inscription");
    }
  }

  async analyzeResume(resume: File): Promise<ResumeAnalysisResponse | null> {
    const formData = new FormData();
    formData.append('file', resume);

    try {
      const response = await axios.post<ResumeAnalysisResponse>(
        `${settings.resumeAnalyzer.baseUrl}/api/extract/?include_raw_text=false`,
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
  public async getCurrentUser(): Promise<ResponseResult<User> | null> {
    return await this.get<ResponseResult<User>>('/me');

    /* const userInfo: UserInfo = {
              ...response.data!,
              name: `${response.data!.firstName} ${response.data!.lastName}`,
              roles: response.data!.roles ?? [],
            };
          }
          return null;*/
  }
}

export const authService = new AuthService();
