# Fix Process List
A basic automation tool used to eliminate or change the priority of certain running processes to take better advantage of the processor usage in the system.

# Details

When starting the program for the first time it will generate a file called `Rules.xml` in the folder, in which you will create the process rules you want.

# Set Your Rules

To define a process rule, the file must follow the following structure:

```xml
<Rules>
	<SetPriority Kind="ByNamePattern" Name="Optional rule name">
		<Param>chrome(\.exe)?</Param>
		<Param>Lowest</Param>
	</SetPriority>

	<KillProcess Kind="ByNamePattern">
		<Param>(GameBar|HelpPane)*</Param>
		<Param>1</Param>
	</KillProcess>

</Rules>
```

Where `SetPriority` will apply the priority of the running process and `KillProcess` it will force kill the process.

The `Kind` attribute refers to the type of filter that will be used. There are some predefined filters:

- `ByNameExact` Searches for the exact name of the process without doing any extra query.

- `ByNamePattern` Searches for the process according to a given regex pattern. (NOTE: The regex by default will have the following characteristics: Ignore case, Culture invariant and ECMAScript compatible)

- `First` Is a special flag that will look for only the first occurrence of the name.

<hr/>
<b>Notes</b>:

- The flags can be combined for example `Kind="ByNameExact, First"` the program will understand that you are looking for the process with that EXACT name and ONLY the FIRST occurrence.

- The `Name` attribute, will better describe what rule does. Useful for error reporting/diagnostics. It's a OPTIONAL attribute. If not defined, will name all rules by `count of rules++`

Each `Param` is a set of params required for that filter. First param is filter in fact, process name or regex.

Second param vary by type of rule:
- `SetPriority` second param is kind of priority (Lowest, Low, Normal, High, Highest)
- `KillProcess` second param is `1` or `0` to indicate whether or not to terminate the process tree or only found process. (Example: Some processes can start legacy processes, if true it will terminate the process and all its child processes)


# Sample Rules

Take an example of my own set of rules:

```xml
<Rules>
	<SetPriority Kind="ByNamePattern">
		<Param>Discord(Canary)?</Param>
		<Param>Lowest</Param>
	</SetPriority>
	
	<SetPriority Kind="ByNamePattern">
		<Param>chrome(\.exe)?</Param>
		<Param>Lowest</Param>
	</SetPriority>
	
	<KillProcess Kind="ByNamePattern">
		<Param>dllhost(\.exe)?</Param>
		<Param>1</Param>
	</KillProcess>
	
	<KillProcess Kind="ByNamePattern">
		<Param>(GameBar|HelpPane).*</Param>
		<Param>1</Param>
	</KillProcess>

	<KillProcess Kind="ByName">
		<Param>msedge</Param>
		<Param>1</Param>
	</KillProcess>
	
	<SetPriority Kind="ByNamePattern">
		<Param>(RuntimeBroker|SearchApp|UserOOBEBroker|Xbox*)(\.exe)?</Param>
		<Param>Lowest</Param>
	</SetPriority>
	
	<KillProcess Kind="ByNamePattern">
		<Param>SystemSettings(Broker)?(\.exe)?</Param>
		<Param>1</Param>
	</KillProcess>
	
</Rules>

```

<hr/>
<b>Notes</b>:

- Don't worry, if one of the rules fails, the program will report ONLY the one that failed and will continue processing the other rules.