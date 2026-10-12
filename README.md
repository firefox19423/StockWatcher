# StockWatcher

Petite application C# qui vérifie des produits sur des boutiques Shopify à quelques minutes d'intervalle et m'envoie une notification sur Discord quand un produit revient en stock.

Projet personnel réalisé pendant mes études en programmation au Cégep Édouard-Montpetit.

## Fonctionnement

- Lit les données publiques du produit sur Shopify (`<url-du-produit>.js`) et vérifie le champ `available`
- Garde en mémoire le dernier statut dans `state.json` pour n'envoyer qu'une seule notification par retour en stock
- Envoie un message par webhook Discord qui me mentionne

Fait avec .NET 9, `HttpClient` et `System.Text.Json`.

## Installation

1. Ajouter ses infos Discord avec **Manage User Secrets** (clic droit sur le projet dans Visual Studio) :
```json
   {
     "Discord": {
       "WebhookUrl": "l'URL de ton webhook",
       "UserId": "ton identifiant Discord"
     }
   }
```
2. Ajouter les produits à surveiller dans `StockWatcher/appsettings.json` :
```json
   "Products": [
     {
       "Name": "Pokemon 30th Celebration ETB",
       "Url": "https://store.401games.ca/products/pokemon-30th-celebration-elite-trainer-box"
     }
   ],
   "CheckIntervalMinutes": 3
```
   - **Name** : le nom qui apparaît dans le message Discord (au choix).
   - **Url** : le lien de la page du produit sur une boutique Shopify. Utiliser la forme courte `/products/...` (enlever `/collections/...` s'il y en a).
   - **CheckIntervalMinutes** : le délai entre chaque vérification. Garder 3 minutes ou plus pour ne pas surcharger la boutique.
3. Lancer avec F5.