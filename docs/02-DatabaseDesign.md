# Database Design

## Core Rule
There is one Animal record for every animal.

Calf, Heifer, Cow, Dry Cow, Sold, and Deceased are stages/statuses, not separate tables.

---

## Table: Animals

Purpose:
Stores the core identity of every animal.

Fields:

- AnimalId
- BarnName
- RegisteredName
- RegistrationNumber
- BirthDate
- Sex
- AnimalStage
- AnimalStatus
- Breed
- SireId
- SireName
- DamId
- DamName
- Notes

---

## Design Rules

- RegistrationNumber can be blank for unregistered calves.
- Animals are never permanently deleted.
- Sold animals remain in history.
- SireId and DamId will link animals together when possible.
- SireName and DamName can be used temporarily when the linked animal is not in the system yet.