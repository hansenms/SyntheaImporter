Synthea FHIR Data Importer
--------------------------

This repository contains an example application which will import FHIR data generated with the [Synthea data generator](https://github.com/synthetichealth/synthea). In processing the data, it does the following:

1. Loops through all the `.json` files in folder provided with the `DataPath` argument.
2. Generates a table of corresponding `fullUrl`, `resourceType`, and `id`
3. Loops through all the `reference` properties in the resources and replaces with a reference corresponding to the resource id in the bundle, e.g. 
    ```json
    {
        "reference": "urn:uuid:babe597e-ff10-4564-9dee-84a505950515"
    }
    ```
    would become:
    ```json
    {
        "reference": "Patient/babe597e-ff10-4564-9dee-84a505950515"
    }
    ```
4. Connects to the FHIR server using provided service client credentials (see below)
5. Loops through the resources and uploads them to the FHIR server.

To use the application, first download and build Synthea, and generate some patients. Then:

```
git clone https://github.com/hansenms/SyntheaImporter
cd SyntheaImporter
dotnet run /DataPath C:\<PATH TO MY SYNTHEA>\synthea\output\fhir /Authority "https://login.microsoftonline.com/<TENANT-ID>" /Audience "https://<IDENTIFIER URI FOR RESOURCE APP>" /ClientId "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX" /ClientSecret "<MY CLIENT SECRET>" /FhirServerUrl "https://<FHIR SERVER URL>"
```