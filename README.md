# Seiren Discord

Synchronise le bouton micro du Razer Seiren V3 Chroma avec le mute de Discord sur Windows. Le pont lit l'état de coupure via Core Audio et vérifie le bouton de Discord via Windows UI Automation. Il ne capture pas de son et ne commande pas Teams.

## Utilisation

1. Sur Windows 64 bits, définissez le Seiren V3 Chroma comme périphérique d'entrée par défaut dans Windows. Sélectionnez cette entrée dans l'application Discord de bureau (version stable, interface en anglais).
2. Lancez **SeirenDiscord.exe**. Il tourne en arrière-plan.
3. Appuyez sur le bouton physique du micro et vérifiez l'icône Discord. Pour arrêter le pont, terminez **SeirenDiscord** dans le Gestionnaire des tâches.

Pour lancer le pont à l'ouverture de session, ajoutez un raccourci vers l'exécutable dans le dossier Démarrage de Windows. Le journal local se trouve dans %LOCALAPPDATA%\SeirenDiscord\SeirenDiscord.log.

## Compilation

Exécutez **build.ps1** dans PowerShell sur Windows 64 bits. Le script utilise le compilateur .NET Framework de Windows et produit **SeirenDiscord.exe** à côté du code source.

## Limites

Le pont suit uniquement les changements du micro physique. Une pression peut être ignorée si Discord est fermé, assourdi (Deafen) ou si ses commandes sont inaccessibles. Au démarrage, le pont ne réactive pas le micro Discord. Si vous changez de périphérique d'entrée, relancez le pont après avoir sélectionné le Seiren. Une mise à jour de Discord peut modifier les noms ou le comportement de ses commandes.
