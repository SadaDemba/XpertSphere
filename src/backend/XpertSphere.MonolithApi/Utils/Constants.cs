namespace XpertSphere.MonolithApi.Utils
{
    public static class Constants
    {
        public const string XPERTSPHERE = "XPERTSPHERE";
        public const string INVALID_EMAIL = "Invalid email address";
        public const string INVALID_ID = "Invalid id";
        public const string ALREADY_DONE = "State already reached";
        public const string CONNECTION_DENIED = "Email or Password Incorrect";
        public const string DEFAULT_PASSWORD = "DefaultP@ssword1";
        public const string ACCESS_DENIED = "Unauthorized acces";
        public const string ACCESS_FORBIDDEN = "Forbidden acces";
        public const string ACTION_DENIED = "Operation denied";
        public const string Error = "An error has occured !";
        public const string LOGGED_IN = "Successfully logged in";
        public const string LOGGED_OUT = "Successfully logged out";
        public const string PASSWORD_CHANGED = "Successfully changed password";
        public const string DELETE_SUCCEEDED = "Successfully deleted";

        public const string COMPANY_NOT_FOUND = "Company not found";
        public const string COMPANY_EXIST = "Company already exist";

        public const string USER_NOT_FOUND = "User not found";
        public const string USER_EXIST = "User already exist";

        public const string SUCCESS_RETRIEVAL = "Data retrieved successfully";
        public const string DATA_NOT_FOUND = "Data  not found";
        public const string OPERATION_FAILED = "Operation failed";
        public const string OPERATION_SUCCEEDED = "Operation completed successfully";
        public const string RESOURCE_NOT_FOUND = "Resource not found";
        public const string RESOURCE_CONFLICTED = "Resource conflict";
        public const string INTERNAL_SERVER_ERROR = "Internal server error";

        // Address validation messages
        public const string STREET_NUMBER_MAX_LENGTH = "Street number cannot exceed 10 characters";
        public const string STREET_NAME_MAX_LENGTH = "Street name cannot exceed 255 characters";
        public const string CITY_MAX_LENGTH = "City cannot exceed 100 characters";
        public const string CITY_INVALID_FORMAT = "City can only contain letters, spaces, hyphens and apostrophes";
        public const string POSTAL_CODE_MAX_LENGTH = "Postal code cannot exceed 20 characters";
        public const string POSTAL_CODE_INVALID_FORMAT = "Invalid postal code format";
        public const string REGION_MAX_LENGTH = "Region cannot exceed 100 characters";
        public const string COUNTRY_MAX_LENGTH = "Country cannot exceed 100 characters";
        public const string COUNTRY_INVALID_FORMAT = "Country can only contain letters, spaces, hyphens and apostrophes";
        public const string ADDRESS_LINE2_MAX_LENGTH = "Address line 2 cannot exceed 255 characters";

        // User validation messages
        public const string FIRST_NAME_REQUIRED = "First name is required";
        public const string FIRST_NAME_MAX_LENGTH = "First name cannot exceed 100 characters";
        public const string FIRST_NAME_INVALID_FORMAT = "First name can only contain letters, spaces, hyphens and apostrophes";
        public const string LAST_NAME_REQUIRED = "Last name is required";
        public const string LAST_NAME_MAX_LENGTH = "Last name cannot exceed 100 characters";
        public const string LAST_NAME_INVALID_FORMAT = "Last name can only contain letters, spaces, hyphens and apostrophes";
        public const string EMAIL_REQUIRED = "Email is required";
        public const string EMAIL_INVALID_FORMAT = "Invalid email format";
        public const string EMAIL_MAX_LENGTH = "Email cannot exceed 255 characters";
        public const string PHONE_NUMBER_INVALID_FORMAT = "Invalid phone number format";
        public const string USER_TYPE_INVALID = "Invalid user type";
        public const string ORGANIZATION_REQUIRED_FOR_INTERNAL_USERS = "Organization is required for internal users";
        public const string EMPLOYEE_ID_REQUIRED_FOR_ORGANIZATIONAL_USERS = "Employee ID is required for organizational users";
        public const string EMPLOYEE_ID_MAX_LENGTH = "Employee ID cannot exceed 50 characters";
        public const string DEPARTMENT_MAX_LENGTH = "Department cannot exceed 100 characters";
        public const string HIRE_DATE_CANNOT_BE_FUTURE = "Hire date cannot be in the future";
        public const string LINKEDIN_PROFILE_INVALID = "Invalid LinkedIn profile URL";
        public const string EXPERIENCE_CANNOT_BE_NEGATIVE = "Experience cannot be negative";
        public const string EXPERIENCE_MAX_YEARS = "Experience cannot exceed 50 years";
        public const string DESIRED_SALARY_MUST_BE_POSITIVE = "Desired salary must be greater than 0";
        public const string DESIRED_SALARY_UNREALISTIC = "Desired salary seems unrealistic";
        public const string AVAILABILITY_CANNOT_BE_PAST = "Availability date cannot be in the past";
        public const string SKILLS_MAX_LENGTH = "Skills description cannot exceed 1000 characters";

        // File upload validation messages
        public const string CV_FILE_REQUIRED = "CV file is required";
        public const string CV_FILE_INVALID = "Invalid file";
        public const string CV_FILE_EXTENSION_INVALID = "File must be one of the following types: .pdf, .doc, .docx";
        public const string CV_FILE_SIZE_EXCEEDED = "File size must not exceed 5MB";
        public const string CV_DESCRIPTION_MAX_LENGTH = "Description cannot exceed 500 characters";

        // Filter validation messages
        public const string DEPARTMENT_FILTER_MAX_LENGTH = "Department filter cannot exceed 100 characters";
        public const string MIN_EXPERIENCE_CANNOT_BE_NEGATIVE = "Minimum experience cannot be negative";
        public const string MIN_EXPERIENCE_MAX_YEARS = "Minimum experience cannot exceed 50 years";
        public const string MAX_EXPERIENCE_CANNOT_BE_NEGATIVE = "Maximum experience cannot be negative";
        public const string MAX_EXPERIENCE_MAX_YEARS = "Maximum experience cannot exceed 50 years";
        public const string MIN_EXPERIENCE_GREATER_THAN_MAX = "Minimum experience must be less than or equal to maximum experience";
        public const string MIN_SALARY_MUST_BE_POSITIVE = "Minimum salary must be greater than 0";
        public const string MAX_SALARY_MUST_BE_POSITIVE = "Maximum salary must be greater than 0";
        public const string MIN_SALARY_GREATER_THAN_MAX = "Minimum salary must be less than or equal to maximum salary";
        public const string SKILLS_FILTER_MAX_LENGTH = "Skills filter cannot exceed 500 characters";
        public const string PAGE_NUMBER_INVALID = "Page number must be a valid positive integer";
        public const string PAGE_SIZE_INVALID = "Page size must be a valid integer between 1 and 100";
        public const string SEARCH_TERMS_MAX_LENGTH = "Search terms cannot exceed 200 characters";
        public const string SORT_FIELD_MAX_LENGTH = "Sort field cannot exceed 50 characters";
        public const string SORT_DIRECTION_INVALID = "Invalid sort direction";

    }
}
