Warts
=====

These are some things that make me sad about the code that I think I can fix.
Note that these are **structural** things related to the interfaces and/or build
environment, not things related to the current work-in-progress implementations.

1.  Too much repetition in translating byte arrays to/from BigInteger.
    -   A specialized struct should encapsulate this.
1.  This is currently x64-only.
    -   I could easily add MSIL support back in if I took advantage of
        conditional compilation and used platform symbols in
        ECDSASignatureChecker.
1.  The version of Rx I use is technically released under a restrictive license.
    -   It's possible to just pull in the Apache-licensed source and build it
        myself.
1.  ITransaction.LockTime requires the caller to know magical things in order to
    use it appropriately.
    -   A specialized struct should encapsulate this.
1.  INetworkPeer has stuff that's useful to IRawNetwork, but there's no
    equivalent for things useful to ICurrencyNetwork.
1.  ITransactionScriptRunner accepts a serialized script, effectively forcing
    implementations to depend on ITransactionScriptParser.
    -   It could just accept a parsed script.