LockFreeDoublyLinkedList
========================

A lock-free doubly linked list implementation in C#.

This work is based on the concept of the paper “Lock-free deques and doubly linked lists”
by Håkan Sundell and Philippas Tsigas (2008).

This project is also available as a [NuGet package](https://www.nuget.org/packages/LockFreeDoublyLinkedList/).

To create the NuGet package from source, install [.Net Core](https://www.microsoft.com/net/learn/get-started/windows) and run the following command:

    dotnet pack --configuration=Release

Tests can be run using:

    dotnet run --project=test/test.csproj
