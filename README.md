**NSoup is currently unmaintained.**

At this time, I am not actively working on this library. However, I will happily accept any help and pull requests, and perhaps return to working on it, should it gain any more traction.

The source code has been migrated from CodePlex in the hopes it will get picked up by the GitHub community. It is by now fairly outdated and perhaps should be ported from latest *jsoup* scratch.

# NSoup
NSoup is a .NET port of the jsoup (https://github.com/jhy/jsoup) HTML parser and sanitizer originally written in Java.

jsoup originally written by [Jonathan Hedley](https://github.com/jhy).
Ported to .NET by Amir Grozki.

**NOTE**: 
(2018-01-09) supported .NET Standard 2.0 by [Milen](https://github.com/milenstack)
(2013-07-10) In the last few months I've been struggling with a few tests crashing for some reason I cannot isolate. I've pushed the latest version of the source code, and if anyone can help solve those issues it would greatly help this project.

## Features

- jQuery-like CSS selectors for finding and extracting data from HTML pages.
- Sanitize HTML sent from untrusted sources.
- Manipulate HTML documents.
