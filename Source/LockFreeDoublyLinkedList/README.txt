The algorithm is based on a paper of Håkan Sundell and Philippas Tsigas.

To create the NuGet package,
build the project using the Release configuration.
Then run the following command:

    nuget.exe pack LockFreeDoublyLinkedList.csproj -Prop Configuration=Release -OutputDirectory NuGet