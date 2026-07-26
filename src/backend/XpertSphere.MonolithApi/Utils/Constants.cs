namespace XpertSphere.MonolithApi.Utils
{
    public static class Constants
    {
        public const string XPERTSPHERE = "XPERTSPHERE";
        public const string INVALID_EMAIL = "Adresse email invalide";
        public const string INVALID_ID = "Identifiant invalide";
        public const string ALREADY_DONE = "État déjà atteint";
        public const string CONNECTION_DENIED = "Email ou mot de passe incorrect";
        public const string DEFAULT_PASSWORD = "DefaultP@ssword1";
        public const string ACCESS_DENIED = "Accès non autorisé";
        public const string ACCESS_FORBIDDEN = "Accès interdit";
        public const string ACTION_DENIED = "Opération refusée";
        public const string Error = "Une erreur est survenue";
        public const string LOGGED_IN = "Connexion réussie";
        public const string LOGGED_OUT = "Déconnexion réussie";
        public const string PASSWORD_CHANGED = "Mot de passe modifié avec succès";
        public const string DELETE_SUCCEEDED = "Suppression réussie";

        public const string COMPANY_NOT_FOUND = "Entreprise introuvable";
        public const string COMPANY_EXIST = "Cette entreprise existe déjà";

        public const string USER_NOT_FOUND = "Utilisateur introuvable";
        public const string USER_EXIST = "Cet utilisateur existe déjà";

        public const string SUCCESS_RETRIEVAL = "Données récupérées avec succès";
        public const string DATA_NOT_FOUND = "Données introuvables";
        public const string OPERATION_FAILED = "Échec de l'opération";
        public const string OPERATION_SUCCEEDED = "Opération réalisée avec succès";
        public const string RESOURCE_NOT_FOUND = "Ressource introuvable";
        public const string RESOURCE_CONFLICTED = "Conflit de ressource";
        public const string INTERNAL_SERVER_ERROR = "Erreur interne du serveur";

        // Address validation messages
        public const string STREET_NUMBER_MAX_LENGTH = "Le numéro de rue ne peut pas dépasser 10 caractères";
        public const string STREET_NAME_MAX_LENGTH = "Le nom de rue ne peut pas dépasser 255 caractères";
        public const string CITY_MAX_LENGTH = "La ville ne peut pas dépasser 100 caractères";
        public const string CITY_INVALID_FORMAT = "La ville ne peut contenir que des lettres, espaces, tirets et apostrophes";
        public const string POSTAL_CODE_MAX_LENGTH = "Le code postal ne peut pas dépasser 20 caractères";
        public const string POSTAL_CODE_INVALID_FORMAT = "Format de code postal invalide";
        public const string REGION_MAX_LENGTH = "La région ne peut pas dépasser 100 caractères";
        public const string COUNTRY_MAX_LENGTH = "Le pays ne peut pas dépasser 100 caractères";

        public const string COUNTRY_INVALID_FORMAT =
            "Le pays ne peut contenir que des lettres, espaces, tirets et apostrophes";

        public const string ADDRESS_LINE2_MAX_LENGTH = "La deuxième ligne d'adresse ne peut pas dépasser 255 caractères";

        // User validation messages
        public const string FIRST_NAME_REQUIRED = "Le prénom est obligatoire";
        public const string FIRST_NAME_MAX_LENGTH = "Le prénom ne peut pas dépasser 100 caractères";

        public const string FIRST_NAME_INVALID_FORMAT =
            "Le prénom ne peut contenir que des lettres, espaces, tirets et apostrophes";

        public const string LAST_NAME_REQUIRED = "Le nom est obligatoire";
        public const string LAST_NAME_MAX_LENGTH = "Le nom ne peut pas dépasser 100 caractères";

        public const string LAST_NAME_INVALID_FORMAT =
            "Le nom ne peut contenir que des lettres, espaces, tirets et apostrophes";

        public const string EMAIL_REQUIRED = "L'email est obligatoire";
        public const string EMAIL_INVALID_FORMAT = "Format d'email invalide";
        public const string EMAIL_MAX_LENGTH = "L'email ne peut pas dépasser 255 caractères";
        public const string PHONE_NUMBER_INVALID_FORMAT = "Format de numéro de téléphone invalide";
        public const string USER_TYPE_INVALID = "Type d'utilisateur invalide";
        public const string ORGANIZATION_REQUIRED_FOR_INTERNAL_USERS = "L'organisation est obligatoire pour les utilisateurs internes";

        public const string EMPLOYEE_ID_REQUIRED_FOR_ORGANIZATIONAL_USERS =
            "L'identifiant employé est obligatoire pour les utilisateurs organisationnels";

        public const string EMPLOYEE_ID_MAX_LENGTH = "L'identifiant employé ne peut pas dépasser 50 caractères";
        public const string DEPARTMENT_MAX_LENGTH = "Le département ne peut pas dépasser 100 caractères";
        public const string HIRE_DATE_CANNOT_BE_FUTURE = "La date d'embauche ne peut pas être dans le futur";
        public const string LINKEDIN_PROFILE_INVALID = "URL de profil LinkedIn invalide";
        public const string EXPERIENCE_CANNOT_BE_NEGATIVE = "L'expérience ne peut pas être négative";
        public const string EXPERIENCE_MAX_YEARS = "L'expérience ne peut pas dépasser 50 ans";
        public const string DESIRED_SALARY_MUST_BE_POSITIVE = "Le salaire souhaité doit être supérieur à 0";
        public const string DESIRED_SALARY_UNREALISTIC = "Le salaire souhaité semble irréaliste";
        public const string AVAILABILITY_CANNOT_BE_PAST = "La date de disponibilité ne peut pas être dans le passé";
        public const string SKILLS_MAX_LENGTH = "La description des compétences ne peut pas dépasser 1000 caractères";

        // File upload validation messages
        public const string CV_FILE_REQUIRED = "Le fichier CV est obligatoire";
        public const string CV_FILE_INVALID = "Fichier invalide";
        public const string CV_FILE_EXTENSION_INVALID = "Le fichier doit être de l'un des types suivants : .pdf, .doc, .docx";
        public const string CV_FILE_SIZE_EXCEEDED = "La taille du fichier ne doit pas dépasser 5 Mo";
        public const string CV_DESCRIPTION_MAX_LENGTH = "La description ne peut pas dépasser 500 caractères";

        // Filter validation messages
        public const string DEPARTMENT_FILTER_MAX_LENGTH = "Le filtre de département ne peut pas dépasser 100 caractères";
        public const string MIN_EXPERIENCE_CANNOT_BE_NEGATIVE = "L'expérience minimum ne peut pas être négative";
        public const string MIN_EXPERIENCE_MAX_YEARS = "L'expérience minimum ne peut pas dépasser 50 ans";
        public const string MAX_EXPERIENCE_CANNOT_BE_NEGATIVE = "L'expérience maximum ne peut pas être négative";
        public const string MAX_EXPERIENCE_MAX_YEARS = "L'expérience maximum ne peut pas dépasser 50 ans";

        public const string MIN_EXPERIENCE_GREATER_THAN_MAX =
            "L'expérience minimum doit être inférieure ou égale à l'expérience maximum";

        public const string MIN_SALARY_MUST_BE_POSITIVE = "Le salaire minimum doit être supérieur à 0";
        public const string MAX_SALARY_MUST_BE_POSITIVE = "Le salaire maximum doit être supérieur à 0";
        public const string MIN_SALARY_GREATER_THAN_MAX = "Le salaire minimum doit être inférieur ou égal au salaire maximum";
        public const string SKILLS_FILTER_MAX_LENGTH = "Le filtre de compétences ne peut pas dépasser 500 caractères";
        public const string PAGE_NUMBER_INVALID = "Le numéro de page doit être un entier positif valide";
        public const string PAGE_SIZE_INVALID = "La taille de page doit être un entier valide compris entre 1 et 100";
        public const string SEARCH_TERMS_MAX_LENGTH = "Les termes de recherche ne peuvent pas dépasser 200 caractères";
        public const string SORT_FIELD_MAX_LENGTH = "Le champ de tri ne peut pas dépasser 50 caractères";
        public const string SORT_DIRECTION_INVALID = "Direction de tri invalide";
    }
}
