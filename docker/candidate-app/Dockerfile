FROM node:24-alpine3.21 AS base

WORKDIR /app

# Copy workspace package files
COPY src/frontend/package*.json ./
COPY src/frontend/.prettierrc ./
COPY src/frontend/.prettierignore ./
COPY src/frontend/eslint.config.mjs ./

# Copy packages (recruiter-app excluded via .dockerignore)
COPY src/frontend/packages ./packages

# Install workspace dependencies
RUN npm ci

# Prepare Quasar for candidate app
RUN npm run postinstall:candidate

# Build arguments for environment variables
ARG VITE_APP_ENV
ARG VITE_WEB_API_BASE_URL
ARG VITE_RESUME_ANALYZER_BASE_URL
ARG VITE_APIM_SUBSCRIPTION_KEY
ARG VITE_APP_VERSION

# Set environment variables for build
ENV VITE_APP_ENV=$VITE_APP_ENV
ENV VITE_WEB_API_BASE_URL=$VITE_WEB_API_BASE_URL
ENV VITE_RESUME_ANALYZER_BASE_URL=$VITE_RESUME_ANALYZER_BASE_URL
ENV VITE_APIM_SUBSCRIPTION_KEY=$VITE_APIM_SUBSCRIPTION_KEY
ENV VITE_APP_VERSION=$VITE_APP_VERSION

# Build the candidate app specifically
RUN npm run build:candidate

# Expose port
EXPOSE 3000

# Start the app
CMD ["npm", "run", "preview:candidate"]