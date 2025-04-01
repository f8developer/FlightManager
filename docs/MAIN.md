# FlightManager Documentation

## Table of Contents
1. [Controllers](#controllers)  
   1.1 [AdminController](#admincontroller)  
   1.2 [FlightsController](#flightscontroller)  
   1.3 [HomeController](#homecontroller)  
   1.4 [ReservationsController](#reservationscontroller)  
   1.5 [ReservationUsersController](#reservationuserscontroller)  
2. [Data Models](#data-models)  
   2.1 [AppUser](#appuser)  
   2.2 [Flight](#flight)  
   2.3 [Reservation](#reservation)  
   2.4 [ReservationUser](#reservationuser)  
3. [Database Context](#database-context)  
   3.1 [ApplicationDbContext](#applicationdbcontext)  
4. [Unit Tests](#unit-tests)  
   4. [ApplicationDbContext Tests](#applicationdbcontext-tests)  
   4.2 [Flight Model Tests](#flight-model-tests)  
   4.3 [Reservation Tests](#reservation-tests)  
   4.4 [ReservationUser Tests](#reservationuser-tests)  
5. [Configuration](#configuration)  
   5.1 [OwnerSettings](#ownersettings)  

---

## Controllers

### AdminController
Handles administrative operations (user/role management).

#### Methods
| Method | Parameters | Description |
|--------|------------|-------------|
| `Index` | `string email`, `string role` | Returns filtered users by email/role. |
| `Roles` | - | Lists all roles with user counts. |
| `Details` | `string id` | Shows user details. |
| `Create` | `AppUser`, `password`, `confirmPassword`, `List<string> selectedRoles` | Creates a user with roles. |
| `Edit` | `string id`, `AppUser`, `List<string> selectedRoles` | Updates user data and roles. |
| `Delete` | `string id` | Deletes a user. |
| `CreateRole` | `IdentityRole role` | Creates a new role. |
| `DeleteRole` | `string id` | Deletes a role. |

---

### FlightsController
Manages flight operations.

#### Methods
| Method | Parameters | Description |
|--------|------------|-------------|
| `Index` | - | Lists all flights. |
| `Details` | `int? id` | Shows flight details. |
| `Create` | `Flight flight` | Creates a flight. |
| `Edit` | `int id`, `Flight flight` | Updates flight data. |
| `Delete` | `int id` | Deletes a flight. |
| `Passengers` | `int id` | Lists passengers for a flight. |

---

### HomeController
Handles general views (home, privacy, error).

#### Methods
| Method | Description |
|--------|-------------|
| `Index` | Returns home page. |
| `Privacy` | Returns privacy policy. |
| `Error` | Returns error page. |

---

### ReservationsController
Manages flight reservations.

#### Methods
| Method | Parameters | Description |
|--------|------------|-------------|
| `Index` | `string id`, `username`, `firstName`, `lastName` | Lists filtered reservations. |
| `Create` | `Reservation reservation` | Creates a reservation. |
| `Edit` | `int id`, `Reservation reservation` | Updates reservation data. |
| `Delete` | `int id`, `bool confirmDeleteUser` | Deletes a reservation. |
| `CheckReservation` | `string egn`, `int flightId` | Validates reservation uniqueness. |

---

### ReservationUsersController
Manages reservation-associated users.

#### Methods
| Method | Parameters | Description |
|--------|------------|-------------|
| `Index` | - | Lists all reservation users. |
| `Create` | `ReservationUser reservationUser` | Creates a reservation user. |
| `Edit` | `int id`, `ReservationUser reservationUser` | Updates reservation user data. |
| `Delete` | `int id` | Deletes a reservation user. |

---

## Data Models

### AppUser
Extends `IdentityUser` for authentication.  
**Properties**:
- `ReservationUsers`: Collection of linked `ReservationUser` entities.

---

### Flight
Represents a flight with validation rules.  
**Properties**:
| Property | Validation Rules |
|----------|------------------|
| `FromLocation` | Required |
| `ToLocation` | Required |
| `DepartureTime` | Required, must be before `ArrivalTime` |
| `BusinessClassCapacity` | Must not exceed `PassengerCapacity` |
| `Reservations` | Collection of reservations |

**Validation**:  
- Ensures `ArrivalTime > DepartureTime`.  
- Ensures `BusinessClassCapacity ≤ PassengerCapacity`.

---

### Reservation
Represents a flight reservation.  
**Properties**:
- `ReservationUser`: Associated user (required).  
- `FlightId`: Linked flight ID (required).  
- `TicketType`: Enum (`Regular`, `Business`).  

**Validation**:  
- Checks for duplicate reservations (same EGN + flight).

---

### ReservationUser
Represents a user making reservations.  
**Properties**:
| Property | Validation Rules |
|----------|------------------|
| `EGN` | 10-digit format |
| `PhoneNumber` | Valid format (e.g., `+359xxxxxxxxx`) |
| `AppUser` | Linked `AppUser` entity |

---

## Database Context

### ApplicationDbContext
Extends `IdentityDbContext<AppUser>` for EF Core.  

**DbSets**:
- `Flights`  
- `Reservations`  
- `ReservationUsers`  

**Key Features**:
- Automatically deletes orphaned `ReservationUser` entries.  
- Configures relationships:  
  ```csharp
  modelBuilder.Entity<Reservation>()
      .HasOne(r => r.ReservationUser)
      .WithMany(u => u.Reservations);
  ```

---

## Unit Tests

### ApplicationDbContext Tests
1. **Orphaned ReservationUser Cleanup**:  
   - Removes `ReservationUser` when last linked reservation is deleted.  
2. **Enum Storage**:  
   - Verifies `TicketType` is stored correctly.  

### Flight Model Tests
1. **Validation Failures**:  
   - Arrival before departure.  
   - Business class exceeds total capacity.  
2. **Valid Data**:  
   - Passes with correct inputs.  

### Reservation Tests
1. **Uniqueness Validation**:  
   - Fails on duplicate EGN + flight.  
   - Fails if DB context is unavailable.  

### ReservationUser Tests
1. **Data Validation**:  
   - Fails on invalid EGN/phone format.  
   - Passes with valid data.  

---

## Configuration

### OwnerSettings
Stores owner credentials for initial setup.  
**Properties**:
- `OwnerEmail`: Email address.  
- `OwnerPassword`: Password (required).  

**Usage**:  
Automatically creates an owner user with `Admin` and `Employee` roles on startup.
