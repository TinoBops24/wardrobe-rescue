# Wardrobe Rescue

Wardrobe Rescue is an AI-powered fashion e-commerce platform designed for the young South African market.

Rather than presenting users with a standard product grid, the platform recommends curated outfit bundles with explainable AI scoring. Users can see why each bundle suits their style, budget, and occasion.

The system was built with ASP.NET Core 9.0, Firebase Firestore, Firebase Authentication, and Google Gemini AI.

---

## Live Site

**URL:** https://wardrobe-rescue-hjh4bzexd8e9hrc7.southafricanorth-01.azurewebsites.net/

---

## Demo Account

| Role | Email | Password |
|------|-------|----------|
| Customer | customer@wardroberescue.co.za | Test123! |

> Administrative access is restricted to protect system data and Firebase resources. Demo admin access can be provided on request.

---

## Core Features

- AI-assisted outfit bundle recommendations
- Explainable scoring based on style, budget, and occasion
- Customer-facing fashion e-commerce interface
- Firebase-backed product and user data
- Authentication using Firebase Auth
- Admin-side product and content management
- Responsive web interface

---

## Tech Stack

- ASP.NET Core 9.0
- C#
- Razor Pages
- Firebase Firestore
- Firebase Authentication
- Google Gemini AI
- Azure App Service
- HTML, CSS, JavaScript

---

## Database and Access Control

The application uses Firebase Firestore as its cloud-hosted database.

Firebase access is restricted for security reasons. Editor access is not publicly available, but can be provided on request for assessment or review purposes.

---

## Running Locally

1. Clone the repository and open it in Visual Studio 2022 or later.

2. Create a local `appsettings.json` file based on `appsettings.example.json`.

3. Update the Firebase credentials path:

```json
"Firebase": {
  "CredentialsPath": "C:\\path\\to\\your\\firebase-credentials.json"
}