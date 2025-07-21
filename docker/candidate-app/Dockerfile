FROM node:24-alpine3.21 AS base

WORKDIR /app

# Copy workspace package files
COPY src/frontend/package*.json ./
COPY src/frontend/.prettierrc ./
COPY src/frontend/.prettierignore ./
COPY src/frontend/eslint.config.js ./

# Copy packages (recruiter-app excluded via .dockerignore)
COPY src/frontend/packages ./packages

# Install workspace dependencies
RUN npm ci

# Prepare Quasar for candidate app
RUN npm run postinstall:candidate

# Build arguments for environment variables
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Build the candidate app specifically
RUN npm run build:candidate

# Expose port
EXPOSE 3000

# Start the app
CMD ["npm", "run", "preview:candidate"]
