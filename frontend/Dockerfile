# Build:  docker build -t crm-web frontend
# Run:    see docker-compose.yml (profile "app")
#
# No API URL is baked in: the app calls relative /api/... paths, and nginx (below)
# proxies them to the backend container. The same image works unchanged from
# localhost, a LAN IP, or a real domain.

FROM node:22-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .

RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
