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

<br/>

## Considerations
While the API has been developed, the following considerations were taken:
* The input currencies can only be one of `eur, gbp, usd` and the price as to be a positive numeric value greater than 0.
* The api's do not have any kind of authentication/authorization mechanism
* Exchange rates cache is implemented as a singleton and its expected to stop the application from starting if it cannot be loaded.

## Future Improvements
In order to complete the exercise withing a reasonable amount of time, some tasks were left undone. With more time these are some of the tasks we should achieve:
* An obvious improvement is around the cache. The one in use is so simple that its really just for demonstration purposes.
  * To go live, we would need to improve its availability, freshness, accessibility (private/shared) and all other know topics around caching
* If the API is intended to be used by large prices, then the aritmetic operation needs to be used in a checked context to eliminate result truncation
* Secure the API by using Oauth2. This would include integrating an Identity Manager as well.
* Replace the static configuration with the dynamic per environment configuration.
* Add a logger to the application for at least non-successful responses. At the moment only the client has the details as to why the API response is not successful.
* Add an exception handling middleware rather than having a try catch in the controller action. For an MVP the try catch is enough.
* Explore other variations the input can have in order to have a more resilient API