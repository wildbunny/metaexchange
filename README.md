metaexchange
============

This is a work in progress, no warrenty is provided for use.

Update from January 19 on Bitsharestalk Forum -->you can follow on https://bitsharestalk.org/index.php?topic=13458.0

Hi everyone, I'm cross-posting this here for better visiblity!

The metaexchange BTC->bitBTC gateway is live for testing!

Here is how to use this

* Import your bitcoin private keys into the bitshares wallet account that you want to use. The private keys must be compressed* (i.e. not starting with a 5, they look like this L4rK1yDtCWekvXuE6oXD9jCYfFNV2cWRpVuPLBcCU2z8TrisoyY1).

* Funds must be sent from a registered bitshares account

* use wallet_account_update_active_key to set one of your imported keys as the active key

* send bitcoins to our gateway address: 1KduukGNb5SH8L6oDwQf8sDrKk68fjvnvF
* 
*check on Blockchain.info https://blockchain.info/address/1KduukGNb5SH8L6oDwQf8sDrKk68fjvnvF

* send bitBTC to our gateway account: metaexchangebtc

Any bitcoins you send will be turned into bitBTC by the gateway (after 1 confirmation) and sent to your bitshares account. Any bitBTC that you send to the gateway will be turned into bitcoins and sent to your bitcoin wallet.

We have funded the gateway with 0.5 BTC/0.5 bitBTC for testing purposes, there is a 0.01 BTC transaction size limit at the moment. Please use small amounts to test this with - this is beta software and may contain bugs, you could lose funds.

For this test there are no transaction fees.

We are well aware that this private key importing process isn't usable for the non-techy, so the next step is to create a simple website to make this procress 100% frictionless, which is what I'll be working on next.

Cheers, Paul.

*) The reason private keys must be compressed is that the bitshares client always converts any private key (compressed or uncompressed) into a compressed public key and since there are two different bitcoin addresses associated with each private key (one from the compressed key, one from the uncompressed version) funds may not arrive in your bitcoin wallet if you import the incorrect type, since the bitshares account public key is turned into a bitcoin address by the daemon.

In case of error, you can import the other version of the private key into your bitcoin wallet to get the funds, but this requires a rescan, which takes a while.
