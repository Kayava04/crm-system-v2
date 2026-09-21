# Build:  docker build -t crm-web --build-arg VITE_API_URL=http://localhost:8080 frontend
# Run:    see docker-compose.yml (profile "app")

FROM node:22-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .

# Baked into the static bundle at build time (Vite only reads import.meta.env.VITE_*
# at build time, not at container start) — this is why it's a build arg, not a
# docker-compose "environment:" entry like the API gets. Must match the origin the
# browser will actually use to reach the API; see VITE_API_URL in .env.example.
ARG VITE_API_URL=http://localhost:8080
ENV VITE_API_URL=$VITE_API_URL

RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
