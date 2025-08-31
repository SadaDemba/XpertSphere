import { UserDto, UserSearchResultDto } from 'src/models/user';

export function convertUserDtoToUserSearchResultDto(userDto: UserDto): UserSearchResultDto {
  const formatExperienceDisplay = (experience?: number): string => {
    if (!experience || experience === 0) return 'No experience';
    if (experience === 1) return '1 year';
    return `${experience} years`;
  };

  return {
    id: userDto.id,
    firstName: userDto.firstName,
    lastName: userDto.lastName,
    email: userDto.email,
    phoneNumber: userDto.phoneNumber!,
    isActive: userDto.isActive,
    createdAt: userDto.createdAt,
    lastLoginAt: userDto.lastLoginAt!,
    organizationId: userDto.organizationId!,
    organizationName: userDto.organizationName!,
    employeeId: userDto.employeeId!,
    department: userDto.department!,
    skills: userDto.skills!,
    experience: userDto.experience!,
    desiredSalary: userDto.desiredSalary!,
    availability: userDto.availability!,
    profileCompletionPercentage: userDto.profileCompletionPercentage!,
    city: userDto.address?.city ?? '',
    country: userDto.address?.country ?? '',
    fullName: userDto.fullName,
    isAvailable: userDto.isActive && userDto.availability !== 'unavailable',
    experienceDisplay: formatExperienceDisplay(userDto.experience),
  };
}
