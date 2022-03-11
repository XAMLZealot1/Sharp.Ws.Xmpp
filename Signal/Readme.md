# Gajim Outgoing Omemo Message

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