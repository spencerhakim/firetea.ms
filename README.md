## Fireteams

### About
Firetea.ms was a minimal, automated matchingmaking system for Destiny, a game available on current- and last-gen Xbox and PlayStation consoles. It was launched in December 2014. Unfortunately it never reached a critical mass of users necessary to make the tool effective, and was shutdown in September 2015.

This code represents the final state of the project before it was shutdown. It will not see any further development.

### Building
- Requires Visual Studio 2013 (or newer), and NuGet
- Should just build as-is

### Deploying
- This will only work on Microsoft Azure, as it targets Cloud Services, Storage, DocumentDB, and ServiceBus
- Look for `####################` in the ServiceConfiguration files and fill in the blanks for with your own keys
