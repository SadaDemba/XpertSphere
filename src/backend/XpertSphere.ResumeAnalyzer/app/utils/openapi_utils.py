import os
import json
from openai import AzureOpenAI
from app.domain.models.resume import Experience, CVModel, Training


def get_llm_gpt_35_turbo() -> AzureOpenAI:
    """
    Initialize and return Azure OpenAI client
    """
    return AzureOpenAI(
        azure_endpoint=os.environ["AZURE_OPENAI_ENDPOINT"],
        api_key=os.environ["AZURE_OPENAI_API_KEY"],
        api_version=os.environ["AZURE_OPENAI_API_VERSION"],
        azure_deployment=os.environ["AZURE_OPENAI_DEPLOYMENT_GPT_35_TURBO"],
    )


def parse_cv_with_openai(cv_text) -> CVModel:
    """
    Extract structured information from CV text using Azure OpenAI
    """
    prompt = f"""
    Extraire précisément les informations suivantes du CV :
    - Prénom
    - Nom
    - Email
    - Numéro de téléphone
    - Profession
    - Adresse
    - Langues
    - Formation
    - Compétences
    - Expériences professionnelles

    Texte du CV :
    {cv_text}

    Réponds UNIQUEMENT au format JSON suivant. Si une information est manquante, mets une chaîne vide ou un tableau vide :
    {{
        "first_name": "",
        "last_name": "",
        "email": "",
        "phone_number": "",
        "profession": "",
        "address": "",
        "languages": [],
        "trainings": [{{
            "school": "",
            "level": ""
            "period": ""
        }}],
        "skills": [],
        "experiences": [{{
            "title": "",
            "description": "",
            "date": ""
        }}]
    }}

    Le champ 'date' peut etre manquant, dans ce cas mets une chaine de caractères vide. Le cas échéant, il doit être dans l'un des formats suivants:
     - 'jj mm aaaa' (si tu as le jour le mois et l'année);
     - 'mm aaaa' (si tu n'as pas le jour);
     - 'aaaa' (si tu ne vois que l'année);
    """
    llm = get_llm_gpt_35_turbo()

    response = llm.chat.completions.create(
        model="gpt-35-turbo",
        messages=[
            {
                "role": "system",
                "content": "Tu es un assistant spécialisé dans l'extraction d'informations de CV.",
            },
            {"role": "user", "content": prompt},
        ],
        response_format={"type": "CVModel"},
    )

    # Parse JSON and create UserModel object
    parsed_data = json.loads(response.choices[0].message.content)

    # Transform each experience into Experience object
    parsed_data["experiences"] = [
        Experience(**exp) for exp in parsed_data["experiences"]
    ]

    parsed_data["trainings"] = [
        Training(**training) for training in parsed_data["trainings"]
    ]

    return CVModel(**parsed_data)
