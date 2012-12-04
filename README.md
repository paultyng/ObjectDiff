ObjectDiff
==========

Diff an object graph (for auditing or human readable purposes)

## Usage

**Note:** these are extension methods as well, but static invocation below is just for example:

```c#
var before = new 
{ 
  Property1 = "", 
	MultilineText = "abc\ndef\nghi", 
	ChildObject = new { ChildProperty = 7 }, 
	List = new string[] { "a", "b" } 
};

var after = new 
{ 
	Property1 = (string)null, 
	MultilineText = "123\n456", 
	NotPreviouslyExisting = "abc", 
	ChildObject = new { ChildProperty = 6 }, 
	List = new string[] { "b", "c" } 
};

string readableDiff = ObjectDiff.ReadableDiffExtensions.ReadableDiff(after, before);
DiffMetadata diff = ObjectDiff.DiffExtensions.Diff(after, before);
```

The value of `readableDiff` would be:

```text
ChildObject - ChildProperty: '6', was '7'
List - [2, added]: 'c', was not present
List - [removed]: No value present, was 'a'
MultilineText: 
-----
123
456
-----
was 
-----
abc
def
ghi
-----
NotPreviouslyExisting: 'abc', was not present
```

A few things to note:

* Property names are replaced by model metadata display name if present
* For lists, object equality uses `Object.Equals` for comparison
* Multiline text is wrapped for formatting purposes
* Objects do not need to be the same runtime type, properties are compared based on naming
* In the readable diff, `null` and `String.Empty` are effectively the same

## On NuGet

http://nuget.org/packages/ObjectDiff

```
PM> Install-Package ObjectDiff
```