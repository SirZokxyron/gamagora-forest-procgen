# gamagora-forest-procgen

Auteur.ices : Hugo BENSBIA, Rose CHAPELLE, Yanis MIOLLANY

## À Propos

Pour ce projet, nous avions à réaliser de la génération procédurale, nous avons donc voulu créer une forêt. Pour cela, nous voulions : avoir un sol qui ne soit pas régulier, des arbres et faire que ceux-ci soient positionnés aléatoirement sans se superposer les uns les autres et en avoir une répartition naturelle.

## Comment l'utiliser

Vous pouvez ouvrir le projet dans Unity. Spécifiquement la scène `SampleScene`.
Le GameObject `Generator` possède tous les scripts nécessaires pour la génération du terrain ainsi que le choix des points pour les arbres. Les options `Default Curve` et `Smooth Curve` vous permettent de visualiser la surface avant sa création pour adapter les paramètres plus simplement.

Actuellement, pour créer un arbre avec le système modulaire de L-System, il vous faut créer un empty GameObject auquel vous attachez le script `LSystem`.
Ensuite il ne reste qu'à remplir l'axiom, les règles et l'interprétation de chaque symbol, ainsi que de gérer les paramètres de rotations et de distance pour un résultat optimal.

## TO-DO

- [ ] Une pré-visualisation de l'arbre pour simplifier le processus de création..
- [ ] La possibilité de mettre des probabilités aux règles du LSystem.
- [ ] Une grammaire paramétrée, cela pourrait permettre de réaliser des animations ou des structures plus complexes.

## Résumé du projet

### Le sol

Dans un premier temps, pour avoir des surfaces lisses au sol, nous avons utilisé les surfaces de Bézier. Ces surfaces permettent de modéliser des formes complexes et fluides en ayant que quelques points de contrôle.
Ce qui a été difficile dans cette réalisation, ça a été de faire la continuité des surfaces de Bézier, car à l’endroit où elles se rejoignent, il est nécessaire de prendre en compte des points de l’autre surface.

### Les arbres

Nous avons ensuite produit les arbres, pour cela, nous avions besoin d'un tronc, de branches, et de feuilles. Pour cela, nous avons réfléchi à plusieurs méthodes. La plus convaincante était celle des L-System. Cette méthode permet de produire de façon procédurale des systèmes reproductibles. Il est donc plutôt intéressant de les utiliser pour les plantes, car ce sont des éléments dans la nature qui se ressemblent. Les L-System fonctionnent de la manière suivante : des éléments de grammaire sont définis, sont ensuite associés à cette grammaire des actions. Ainsi, il est possible de définir n’importe quelle règle qui pourra être reproduite un nombre défini de fois. Ce qui a pu être difficile est de trouver l’angle de rotation des branches par rapport au tronc, du fait de notre positionnement dans un repère qui n’est pas le même que celui du monde initial. Le défi a aussi été de créer un système modulaire de L-System, c'est-à-dire que nous avions la possibilité de modifier le sens des lettres dans l’interface, tout en modifiant les règles à effectuer.

### Le choix des positions pour les arbres

Une fois que nous avions ces deux éléments, nous avions besoin de positionner les arbres de manière aléatoire sans qu’ils ne se superposent et de façon à reproduire le style d’une forêt.
Pour ce faire, nous avons utilisé les disques de Poisson, que nous avons poussé plus loin en utilisant le bruit de Perlin pour faire varier la distance minimale entre les arbres.
Cet algorithme utilise la loi uniforme. Son fonctionnement est le suivant :

- Il faut tout d’abord positionner un point
- Choisir une position aléatoire autour de ce point
- Vérifier qu’il n’y a pas d’autres points trop proches de lui
- Ajouter ce point dans la liste des points à traiter
- Pour finalement recommencer le processus pour positionner un point autour de ce nouveau point.

Ce processus est fait un nombre precis (20 dans notre cas) de fois pour chaque points à traiter.
Maintenant nous pouvons jouer à trouver les bons paramètres (distance minimale par défaut, taille du bruit de Perlin, etc) pour obtenir le resultat qui nous convient.

### Tout assemblé

À la fin, pour lier tous ces éléments, le chemin de construction est le suivant : une grille est générée d’une taille définie, cette grille à ses points de contrôles qui sont modifiés en hauteur pour créer des surfaces non planes. Dans cet espace, des points sont récupérés grâce au disque de poisson, et de ces points, on récupère leur hauteur pour positionner un arbre à ce point.
