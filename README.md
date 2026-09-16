# Symulacja ruchu drogowego w silniku Unity z wykorzystaniem architektury DOTS
### Informacje techniczne
Wersja Unity: 6000.0.80f1
### Cel projektu
Celem projektu jest opracowanie symulacji ruchu drogowego, umożliwiającego
modelowanie zachowań pojazdów w środowisku wirtualnym. System pozwoli na
symulację ruchu wielu pojazdów poruszających się po sieci dróg, zmierzających do
wyznaczonego celu oraz reagujących na elementy infrastruktury drogowej, takie jak
skrzyżowania czy sygnalizacja świetlna. Pojazdy będą mogły dostosowywać się do
dynamicznych warunków na drodze, m.in. wyprzedzać inne pojazdy oraz
wyszukiwać objazdy w przypadku nieoczekiwanego zamknięcia skrzyżowania lub
innych przeszkód.
Użytkownik będzie miał możliwość tworzenia własnej sieci dróg lub wczytania
wcześniej zdefiniowanych map. Podczas projektowania dróg i skrzyżowań będzie
można modyfikować właściwości poszczególnych odcinków, takie jak ograniczenia
prędkości, zakaz wyprzedzania, kierunek jazdy,pierwszeństwo przejazdu czy czasy
zmian świateł.
Dodatkowo system umożliwi dostosowywanie właściwości zarówno kierowców, jak i
pojazdów, np. stylu jazdy, przyspieszenia, hamowania czy maksymalnej prędkości.
### Aktualny stan prac
- Edytor dróg i skrzyżowań – umożliwia tworzenie podstawowej sieci drogowej oraz jej modyfikowanie.
- Generator SubScen – umożliwia tworzenie i zarządzanie SubScenami przeznaczonymi dla Unity DOTS oraz przenoszenie utworzonej w edytorze sieci dróg do SubScene w celu konwersji obiektów GameObject na Entities.
- Prosty spawner pojazdów – umożliwia generowanie pojazdów na utworzonej sieci drogowej.
- Podstawowy ruch pojazdów – pojazdy poruszają się obecnie w sposób losowy po utworzonym grafie dróg, wybierając kolejne dostępne połączenia.

### Funkcjonalności w trakcie realizacji
