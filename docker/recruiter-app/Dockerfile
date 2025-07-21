FROM node:24-alpine3.21 AS base

WORKDIR /app

# Copy workspace package files
COPY src/frontend/package*.json ./
COPY src/frontend/.prettierrc ./
COPY src/frontend/.prettierignore ./
COPY src/frontend/eslint.config.mjs ./

# Copy packages (candidate-app excluded via .dockerignore)
COPY src/frontend/packages ./packages

# Install workspace dependencies
RUN npm ci

# Prepare Quasar for recruiter app
RUN npm run postinstall:recruiter

# Build arguments for environment variables
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Build the recruiter app specifically
RUN npm run build:recruiter

# Expose port
EXPOSE 3001

# Start the app
CMD ["npm", "run", "preview:recruiter"]
