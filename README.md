# Meeg.Configuration

The purpose of this library is to provide a configuration API for .NET Framework projects that matches parts of the configuration API found in .NET Core. Specifically, the API provides equivalents to:

* [GetValue](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#getvalue)
* [GetSection, GetChildren, and Exists](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#getsection-getchildren-and-exists)
* [Bind to a class](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#bind-to-a-class)
* [Bind to an object graph](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#bind-to-an-object-graph)
* [Bind an array to a class](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#bind-an-array-to-a-class)

> 🙋 The intention is to use this library in tandem with [Configuration builders in ASP.NET](https://docs.microsoft.com/en-us/aspnet/config-builder) as they serve the role that [Providers](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#providers) do in a .NET Core project, but use of Configuration Builders is not a pre-requisite for using this library.
>
> Please also note that this library targets a minimum of .NET Framework 4.6, but you will need to be using a minimum of .NET Framework 4.7.1 to use Configuration builders.

## Getting started

To get started:

* Install the NuGet package `Meeg.Configuration`
* (Optional) Set up [Configuration Builders](https://docs.microsoft.com/en-us/aspnet/config-builder) for your application
* Start [using the library](#usage) in your application code

> 🙋 The configuration API exposed by this library assumes that you are using the same conventions as used by .NET Core Configuration when it comes to [keys](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#keys) and [hierarchical data](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#hierarchical-configuration-data). If you are not then some features of the library may not work as intended.

## Usage

The main entry point to the API is the `AppConfiguration` type. To create a new `AppConfiguration` instance you will first need to create an instance of `ConfigurationManagerAdapter`, which is an adapter for .NET Framework's `ConfigurationManager` and is a required dependency. For example:

```c#
using Meeg.Configuration;

var configManager = new ConfigurationManagerAdapter();
var config = new AppConfiguration(configManager);
```

> If you have a non-trivial application that requires access to configuration in multiple places you may wish to wrap this into a singleton, create an extension or static "utility" method, or register it as a service in your IoC container. If the latter, you can register it with a singleton lifetime scope.

Once you have an `AppConfiguration` instance you can start using the features of this library to work with `AppSettings`:

```c#
// Get values by key
string value = config["Key"];

// Type conversion of configuration keys with support for default value if the key doesn't exist
int value = config.GetValue<int>("Key");
bool otherValue = config.GetValue<bool>("OtherKey", false);

// Get configuration "sections" - sections are delineated using a colon
var section = config.GetSection("Section");
var subsection = config.GetSection("Section:Subsection");

// Build section keys
string sectionKey = AppConfigurationPath.Combine("Section", "Subsection");
var section = config.GetSection(sectionKey);

// Get child sections
var children = config.GetChildren();

// You can also get values, sections and children of a section, and check if a section exists (has a value or children)
var section = config.GetSection("Section");
string value = section["Key"];
int otherValue = section.GetValue<int>("OtherKey");
var subsection = section.GetSection("Subsection");
var children = section.GetChildren();
bool exists = section.Exists();

// Bind to an instance of a type (supports object graphs, collections and arrays)
var foo = new Foo();
config.GetSection("Foo").Bind(foo);

// Or create and bind to an instance of a specified type
var bar = config.GetSection("Foo").Get<Foo>();
```

You can also use it to access `ConnectionStrings`:

```c#
string connectionString = config.GetConnectionString("ConnectionStringName");
```

## FAQ

### Why not just use the .NET Core Configuration libraries in your .NET Framework app?

This is a question I asked myself when building this library as it is [completely possible](https://benfoster.io/blog/net-core-configuration-legacy-projects) to do so and would preclude the need to for this library to exist.

The reason I persisted in the end was because:

* I wanted to use the Configuration Builders feature provided by the Framework because other parts of my application were already reliant on `ConfigurationManager`; but
* I also wanted to use the configuration API that existed in .NET Core and there is nothing like it in the Framework
* Introducing the .NET Core libraries would introduce a second and separate configuration mechanism, which would be able to consume configuration settings exposed by `ConfigurationManager`; but
* The parts of my application that were already using `ConfigurationManager` would be unaware of this separate configuration API
* Ultimately I wanted to stay closer to and use the features made available by the Framework

At least now with the existence of this library I have both options, and with the APIs being so similar it should be easier to switch over if I migrate to .NET Core (or if I change my mind again)!

### What features of .NET Core Configuration aren't implemented by this library?

* [Providers](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#providers) are not a part of this library - [Configuration builders](https://docs.microsoft.com/en-us/aspnet/config-builder) can be used instead
* "Reload on change" is a feature of some Providers, but is not implemented by this library or by Configuration builders

### What about the Options pattern?

This library does not provide any equivalent to the [Options pattern in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-3.1).

## Contributing

Please see the [contributing](CONTRIBUTING.md) doc.

## License

Licensed under the [MIT License](LICENSE).

This work is derived from work found in the [.NET Extensions](https://github.com/dotnet/extensions/blob/master/LICENSE.txt) project. Thank you to the owners and contributors of that project for the work on which this project is based!
