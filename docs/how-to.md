# How to accomplish common tasks with the SARIF SDK

* [Write a SARIF log file to disk](#write-a-SARIF-log-file-to-disk)
* [Read a SARIF log file from disk](#read-a-SARIF-log-file-from-disk)
* [Format a result message](#format-a-result-message)
* [Add a property to an object's property bag](#add-a-property-to-an-objects-property-bag)
* [Retrieve a property from an object's property bag](#retrieve-a-property-from-an-objects-property-bag)

## Write a SARIF log file to disk

Whenever you read or write a SARIF log file, use the `SarifContractResolver` class to format elements such as URIs and dates according to the SARIF standard.

```C#
JsonSerializerSettings settings = new JsonSerializerSettings()
{
    ContractResolver = SarifContractResolver.Instance,
    Formatting = Formatting.Indented
};

SarifLog log = ... ;

sarifText = JsonConvert.SerializeObject(log, settings);
File.WriteAllText(outputFilePath, sarifText);
```

## Read a SARIF log file from disk

Whenever you read or write a SARIF log file, use the `SarifContractResolver` class to format elements such as URIs and dates according to the SARIF standard.

```C#
string logContents = File.ReadAllText(logFilePath);

var settings = new JsonSerializerSettings()
{
    ContractResolver = SarifContractResolver.Instance
};

SarifLog log = JsonConvert.DeserializeObject<SarifLog>(logContents, settings);
```

## Format a result message

```C#
Result result = ...
IRule rule = ...

// GetMessageText is an extension method on the Result class
string resultMessage = result.GetMessageText(result, rule);

```

## Add a property to an object's "property bag"

You can do this for any object that has a property bag (that is, for any instance of a class derived from `PropertyBagHolder`),
such as `Run`, `Result`, `Location`, `Rule`, _etc._

```C#
Result result = ... ;

// Add a string-valued property:
result.SetProperty("category", "security");

// Add an integer-valued property:
result.SetProperty("occurrences", 42);

// Add a property of arbitrary type:
MyClass myObject = new MyClass(54, "stuff", "otherStuff");
result.SetProperty("myclass", myObject);

// Add a property with a null value (but then you have to specify
// the type:
result.SetProperty<string>("category", null);
```

## Retrieve a property from an object's "property bag"

You can do this for any object that has a property bag (that is, for any instance of a class derived from `PropertyBagHolder`),
such as `Run`, `Result`, `Location`, `Rule`, _etc._

```C#
Result result = ... ;

// Retrieve a string-valued property:
string category = result.GetProperty("category");

// Retrieve an integer-valued property:
int occurrences = result.GetProperty<int>("occurrences");

// Retrieve a property of arbitrary type:
MyClass myObject = result.GetProperty<MyClass>("myclass", myObject);

// WRONG: Don't use the generic version to retrieve a string-valued property:
// string category = result.GetProperty<string>("category");
```
