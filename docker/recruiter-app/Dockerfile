FROM node:24-alpine3.21 AS base
# Set working directory
WORKDIR /app

# Copy package.json
COPY src/frontend/packages/recruiter-app/package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY src/frontend/packages/recruiter-app/ .

# Build arguments for environment variables
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Build the app
RUN npm run build

# Expose port
EXPOSE 3000

# Start the app (Quasar can serve the built files)
CMD ["npm", "run", "preview"]