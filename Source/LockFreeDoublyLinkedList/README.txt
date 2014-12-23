The algorithm is based on a paper of Håkan Sundell and Philippas Tsigas.

To create the NuGet package,
build the project using the Release configuration.

Run the following commands in the current path where this README is located:

    mkdir NuGet
    D:\Path\to\nuget.exe pack LockFreeDoublyLinkedList.csproj -Prop Configuration=Release -OutputDirectory NuGet