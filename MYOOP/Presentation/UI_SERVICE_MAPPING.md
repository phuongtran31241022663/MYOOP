# UI ↔ Backend Service Mapping

This document maps UI elements to their corresponding backend services and business logic.

## 1. Authentication

| UI Element | Backend Service | Method | Description |
|------------|-----------------|--------|-------------|
| LoginForm | IUserService | `Login(phone, password)` | Authenticate user |
| RegisterForm | IUserService | `RegisterPassenger(...)` | Create new passenger |
| RegisterForm | IUserService | `RegisterDriver(...)` | Create new driver |
| ProfileForm | IUserService | `ChangePassword(...)` | Update password |

## 2. Trip Management

| UI Element | Backend Service | Method | Description |
|------------|-----------------|--------|-------------|
| RequestTripForm | ITripService | `RequestTrip(...)` | Create new trip request |
| RequestTripForm | IFareService | `CalculateFare(trip)` | Calculate trip fare |
| DriverTripForm | ITripService | `AssignDriver(...)` | Assign driver to trip |
| DriverTripForm | ITripService | `MarkArrived(driverId)` | Mark driver arrived |
| DriverTripForm | ITripService | `StartTrip(tripId)` | Start the trip |
| DriverTripForm | ITripService | `CompleteTrip(tripId)` | Complete the trip |
| TripHistoryForm | ITripService | `GetTripHistory(userId)` | Get user's trip history |
| RatingForm | IRatingService | `CreateRating(...)` | Rate completed trip |

## 3. Driver Operations

| UI Element | Backend Service | Method | Description |
|------------|-----------------|--------|-------------|
| DriverDashboardForm | IUserService | `UpdateDriverStatus(...)` | Update driver availability |
| DriverDashboardForm | IUserService | `UpdateDriverLocation(...)` | Update driver GPS location |
| DriverDashboardForm | ITripService | `GetAvailableTripsForDriver(...)` | Get available trips |
| DriverDashboardForm | ITripService | `RejectTrip(...)` | Driver rejects a trip |

## 4. Search & Location

| UI Element | Backend Service | Method | Description |
|------------|-----------------|--------|-------------|
| MapControl | IRouteService | `GetFullRouteAsync(...)` | Get route between locations |
| MapControl | IRouteService | `CalculateDistanceAsync(...)` | Calculate distance |
| LocationCard | - | View Only | Display location info |
| TripCard | - | View Only | Display trip info |

## 5. Admin Operations

| UI Element | Backend Service | Method | Description |
|------------|-----------------|--------|-------------|
| AdminDashboardForm | IAdminService | `GetAllTrips()` | Get all trips |
| AdminDashboardForm | IAdminService | `GetAllUsers()` | Get all users |
| AdminDashboardForm | IAdminService | `DeactivateUser(...)` | Deactivate user account |
| AdminDashboardForm | IFareService | `UpdateFareRule(...)` | Update fare rules |

## 6. Notification Flow

```
Backend Events          →    NotificationService    →    UI Updates
─────────────────────────────────────────────────────────────────
TripRequestedEvent     →    NotifyDriver()        →    DriverForm popup
TripMatchedEvent      →    NotifyPassenger()     →    PassengerForm popup  
TripArrivedEvent      →    NotifyTripUpdate()    →    Status update
TripCompletedEvent    →    NotifyTripUpdate()    →    Rating prompt
```

## 7. Recommended UI Components

### Controls
- **LocationCard** (`Presentation/Controls/LocationCard.cs`) - Display pickup/destination
- **TripCard** (`Presentation/Controls/TripCard.cs`) - Display trip in list

### Container Patterns
- **FlowLayoutPanel** - For scrollable lists (trip history, search results)
- **TableLayoutPanel** - For structured forms (registration, profile)
- **Panel with Card styling** - For grouped content

### Form Standards
- Use **BaseDialogForm** for modal dialogs (FixedDialog, CenterParent)
- Use **BaseDashboardForm** for main views (Sizable, MinSize enforced)
- All forms should use **AppTheme** constants for colors, fonts, and spacing

## 8. User Interaction Flow

### Search Location Flow
1. User types in TextBox → calls local search API
2. Results displayed in **FlowLayoutPanel** with **LocationCard** controls
3. User clicks map → map returns lat/lng → reverse geocode
4. **LocationCard** updates with selected location

### Request Trip Flow
1. User selects pickup LocationCard
2. User selects destination LocationCard  
3. System calculates fare via **IFareService**
4. User clicks "Đặt xe" → **ITripService.RequestTrip()**
5. System searches for driver via **IDriverMatchingService**
6. On match, notification sent to driver
