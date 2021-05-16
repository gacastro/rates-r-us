# Rates"R"Us

We have decided that to further international expansion we want to offer more currencies. To do that we need a new API for converting prices.

To begin this effort, our Product Owner has come to you to help develop a minimum viable product (MVP). It is expected that this MVP, if successful, will be built upon with additional features into a full product.
They have suggested that the API will receive a price, source currency and target currency. It should return the price converted to the target currency and target currency.
The currency exchange rates must be the latest available rates and fetched from the exchange rates API provided. The docs are available [here](https://trainlinerecruitment.github.io/exchangerates/)
<br/>
<br/>

## Table of contents

- [Rates"R"Us](#ratesrus)
  - [Table of contents](#table-of-contents)
  - [Considerations](#considerations)
  - [Future Improvements](#future-improvements)
  - [How to run](#how-to-run)

<br/>

## Considerations
While the API was developed, the following considerations were taken:
* The input currencies have to be one of `eur, gbp, usd` and the price as to be a numeric value greater than 0.
* The api does not have authentication/authorization mechanisms.
* There is an exchange rates cache that was implemented as a singleton and its expected to prevent the application from starting if it cannot be loaded.
* The cache has a configurable refresh rate and it has to be greater than 0. Currently its been set to refresh after 60 days so that we can benefit while using the provided api.
* Input is being validated at the controller making it unnecessary to validate further
* NewtonSoft library has been chosen over the System.Text.Json to handle Json. More devs that know it and it doesn't require extra configuration to work with base types. Like long to which the api depends

## Future Improvements
In order to complete the exercise within a reasonable amount of time, some tasks were left undone. With more time, these are some of the tasks I'd still like to achieve:
* An obvious improvement is around the cache. The one in use is so simple that its really just for demonstration purposes.
  * To go live, we would need to improve its availability, freshness, accessibility (private/shared) and all other known topics around caching
* If the API is intended to be used by large prices, then the aritmetic operation needs to be used in a checked context to avoid result truncation
* Secure the API by using Oauth2. This would include integrating an Identity Manager as well.
* Replace the static configuration with a dynamic configuration per environment. One option would be to use the the configuration providers available with NetCore
* Add a logger to the application. At the moment its only the client that has the failure details.
* Add an exception handling middleware rather than having a try catch in the controller. Considerig its just one controller (better yet, one action) one try catch is enough.
* Although I tried to be as exhaustive as I could, there are always ways to surprises us when it comes to how the input is sent. I'd increase the monitoring around it to ensure we can learn those ways and then implement preventive measures
* Handle better the responses from the provided API. For instance, against unexpected response schemas, like missing, or wrong type, of parameters. Invalid values like rates of 0 or unknown currencies. 400 responses. And so on
* 404 at the moment is being used to state when a currency is not found or that its expired. We could make the api less ambiguous by returning another status code for expired
* Add `Swashbuckle` to generate the api documentation but configured to only generate for lower environments

## How to run
.Net core needs to be installed in your machine.

From your command line navigate to the root folder of the application and:
```
> cd API
> dotnet run
```

An example request is:
```
curl --location --request POST 'http://localhost:5000/exchange' \
--header 'Content-Type: application/json' \
--data-raw '{
    "price": 23.32,
    "source": "EUR",
    "target": "USD"
}'
```
And the corresponding response is:
```
{
    "exchange": 27.61,
    "exchangeIn": "USD",
    "exchangeRate": 1.183894
}
```