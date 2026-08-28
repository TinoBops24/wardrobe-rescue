# Wardrobe Rescue

Wardrobe Rescue is an AI-powered fashion commerce platform that helps shoppers decide what to wear and what to buy. Instead of starting with an overwhelming product grid, shoppers describe an occasion, style, budget, or clothing item and receive curated outfit bundles with explainable recommendations.

## Live demo

[Open Wardrobe Rescue](https://wardrobe-rescue-hjh4bzexd8e9hrc7.southafricanorth-01.azurewebsites.net/)

The platform is designed for the South African market, with product pricing and shopping flows presented in South African rand. The public demo is available for evaluation; administrator access is intentionally not included in this repository.

## Product highlights

- AI Stylist that converts a natural-language brief into styling constraints
- Image-assisted styling for uploaded clothing items
- Curated outfit bundles for occasions such as interviews, date nights, and smart-casual events
- Explainable bundle scoring based on style, occasion, budget, and product compatibility
- Product catalogue, product details, wishlist, cart, and checkout flows
- Firebase Authentication for customer accounts
- Persistent chat sessions for signed-in customers
- Protected admin portal for products, bundles, orders, customers, and reports
- Responsive storefront and dedicated AI Stylist workspace

## Engineering highlights

- Service boundaries separate AI integration, authentication, Firestore access, and outfit-bundle generation.
- Input validation limits prompts and uploaded images before they reach the AI service.
- Product and bundle data is cached in memory to reduce repeated Firestore reads.
- Admin pages are protected centrally through an authorization filter.
- Azure deployment uses GitHub Actions with OpenID Connect, so cloud credentials are not stored in the repository or printed in workflow logs.

## Technology

| Area | Technology |
| --- | --- |
| Application | C#, ASP.NET Core 9, Razor Pages |
| AI | Google Gemini API, with `gemini-3.1-flash-lite` as the default model |
| Authentication | Firebase Authentication |
| Data | Cloud Firestore |
| Frontend | Razor views, HTML, CSS, JavaScript |
| Hosting | Azure App Service |
| CI/CD | GitHub Actions |

## Deployment

Every push to `development` runs the deployment workflow. It restores the .NET application, publishes `WardrobeRescue.csproj`, and deploys the result to the Azure App Service development environment.

Deployment configuration lives in [`deploy-development.yml`](.github/workflows/deploy-development.yml). The workflow uses the GitHub `development` environment and Azure federated identity credentials rather than long-lived Azure passwords or service-principal secrets.

## Run locally

### Prerequisites

- .NET 9 SDK
- A Firebase project with Firestore and Firebase Authentication enabled
- A Google Gemini API key
- Visual Studio 2022 or a compatible .NET development environment

### Setup

1. Clone the repository and open the solution:

   ```bash
   git clone https://github.com/TinoBops24/wardrobe-rescue.git
   cd wardrobe-rescue
   ```

2. Create `appsettings.json` from [`appsettings.example.json`](appsettings.example.json).

3. Add your local Firebase service-account path and Gemini API key to your local configuration. Never commit service-account files, API keys, or production credentials.

4. Restore and run the application:

   ```bash
   dotnet restore
   dotnet run --launch-profile https
   ```

5. Open the local URL shown by the .NET CLI.

## Project structure

```text
Pages/              Razor Pages for storefront, account, assistant, checkout, and admin
Services/            AI, Firebase Auth, Firestore, and bundle-generation services
Models/              Firestore and application models
Helpers/             Session, cart, and authentication helpers
Filters/             Centralized admin authorization
wwwroot/             Site styles, scripts, and static assets
.github/workflows/   Azure deployment workflow
```

## Why this project is interesting

Wardrobe Rescue combines a real e-commerce journey with an AI recommendation workflow. The key product challenge is turning an ambiguous request such as “I need something polished for an interview” into useful, purchasable, and explainable outfit recommendations. The application addresses that through structured AI constraints, deterministic bundle rules, product scoring, and a checkout-ready shopping flow.

## License

This project is currently presented as a portfolio project. Licensing details can be added when the project is prepared for external reuse.
