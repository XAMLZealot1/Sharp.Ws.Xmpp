# Gajim Outgoing OMEMO Message

```
	<!-- Outgoing 2/27/2022 2:42:09 PM (test1@example.org) -->
	<message xmlns="jabber:client" to="test2@example.org" type="chat" id="4a8b8dac-acbc-4166-a2fb-286c7561bfa9">
		<request xmlns="urn:xmpp:receipts" />
		<active xmlns="http://jabber.org/protocol/chatstates" />
		<markable xmlns="urn:xmpp:chat-markers:0" />
		<origin-id xmlns="urn:xmpp:sid:0" id="4a8b8dac-acbc-4166-a2fb-286c7561bfa9" />
		<encrypted xmlns="eu.siacs.conversations.axolotl">
			<header sid="1797815022">
				<key rid="697255201">MwohBcJzhALU5ZiBSn8RqMR6SJNe2TB3i9IGypFm/yhmZztlEAAYACIwScgGuN2u1xeY19JZA3DLoLWlyAj8LyTMRk1uO6IEcUkMoCmpR2H6e83SK62b0dj0Rj1u/AZoWNY=</key>
				<iv>8/pXK3Y8y8gKuVFx</iv>
			</header>
			<payload>5Ro9Hc0DCWBk+Uq7wkCgY9tM4Ecn10njWw==</payload>
		</encrypted>
		<encryption xmlns="urn:xmpp:eme:0" name="OMEMO" namespace="eu.siacs.conversations.axolotl" />
		<body>You received a message encrypted with OMEMO but your client doesn't support OMEMO.</body>
		<store xmlns="urn:xmpp:hints" />
	</message>
```

# Conversation Outgoing OMEMO Message

```
<!-- Incoming 3/11/2022 11:01:39 AM (test1@example.org) -->
<message xmlns="jabber:client" from="donnie@hyatts.net/Conversations.lqeK" to="test1@example.org" id="d6d0e216-031d-44ae-8b37-fdd7e711117c" type="chat">
  <body>I sent you an OMEMO encrypted message but your client doesn’t seem to support that. Find more information on https://conversations.im/omemo</body>
  <encrypted xmlns="eu.siacs.conversations.axolotl">
    <header sid="252549526">
      <key rid="2004970878" prekey="true">MwhKEiEFAS1bqfTo4pg+48j541v3uUNK/BHTWqtcRG2nuRyy5XUaIQXvXttNDHiJUps0gN5sJkZdTYbwA4UHlzN7DeWKaCgFCSJiMwohBfbyefXvdNpPYZkI81fu7LXZOYh/OiKrX1bSKFvKb/wWEAIYACIwTYo7su0DTDB+afS7zi6+u4y2pTPS3rxQ6Ru7kCiQStlkUD22PoP8vBhg9cpB19C6/YNXS/DdWqAolrO2eDAB</key>
      <key rid="1798462645">MwohBcaYtzCqRgqpDiulXkLsQu+n3tBw/lAZUIgRbN9fNqEcEAAYACIwsgsMqmzqbv+ymm7JIvs5+FQroELMth4SZGveaFetYIJE/+Zo9w56VdDMiYCrwfwQ/EBdXZmpvr8=</key>
      <key rid="893018010" prekey="true">MwjvzpzSAxIhBVLLS6g2MUYeNbpiryMHMmBDRR0Wu4vEHGkqcqvps+46GiEF717bTQx4iVKbNIDebCZGXU2G8AOFB5czew3limgoBQkiYjMKIQVGY7I4GwX7b2zhWwWNRMFDNDVnCgntOqL9WLzSOsuWShAAGAAiMK4AQ9sxnVbhw7eyK9KvAfwN5TiAsa+5+lVeTxFafGm0TAmpE2NeZ+62nbRla1hxHN7oMASyc6hlKJaztngwyNLyqQU=</key>
      <iv>glm/WwCzyeth4wuX</iv>
    </header>
    <payload>LuH6</payload>
  </encrypted>
  <request xmlns="urn:xmpp:receipts" />
  <markable xmlns="urn:xmpp:chat-markers:0" />
  <origin-id id="d6d0e216-031d-44ae-8b37-fdd7e711117c" xmlns="urn:xmpp:sid:0" />
  <store xmlns="urn:xmpp:hints" />
  <encryption name="OMEMO" xmlns="urn:xmpp:eme:0" namespace="eu.siacs.conversations.axolotl" />
</message>
```