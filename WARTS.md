Warts
=====

These are some things that make me sad about the code that I think I can fix.
Note that these are **structural** things related to the interfaces and/or build
environment, not things related to the current work-in-progress implementations.

1.  Too much repetition in translating byte arrays to/from BigInteger.
    -   A specialized struct should encapsulate this.
1.  ITransaction.LockTime requires the caller to know magical things in order to
    use it appropriately.
    -   A specialized struct should encapsulate this.
1.  INetworkPeer has stuff that's useful to IRawNetwork, but there's no
    equivalent for things useful to ICurrencyNetwork.
