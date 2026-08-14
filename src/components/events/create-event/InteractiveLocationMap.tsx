import { useEffect } from 'react'
import { MapContainer, Marker, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import { divIcon, type LeafletMouseEvent, type Marker as LeafletMarker } from 'leaflet'
import 'leaflet/dist/leaflet.css'

interface Coordinates {
  latitude: number
  longitude: number
}

interface InteractiveLocationMapProps extends Coordinates {
  disabled?: boolean
  onPick: (coordinates: Coordinates) => void
}

const pinIcon = divIcon({
  className: 'event-map-pin',
  html: '<span aria-hidden="true"></span>',
  iconAnchor: [14, 28],
  iconSize: [28, 28],
})

function MapInteraction({ disabled, onPick }: Pick<InteractiveLocationMapProps, 'disabled' | 'onPick'>) {
  useMapEvents({
    click(event: LeafletMouseEvent) {
      if (!disabled) onPick({ latitude: event.latlng.lat, longitude: event.latlng.lng })
    },
  })
  return null
}

function RecenterMap({ latitude, longitude }: Coordinates) {
  const map = useMap()
  useEffect(() => {
    map.setView([latitude, longitude], Math.max(map.getZoom(), 17))
  }, [latitude, longitude, map])
  return null
}

export default function InteractiveLocationMap({
  latitude,
  longitude,
  disabled = false,
  onPick,
}: InteractiveLocationMapProps) {
  const handleDragEnd = (event: { target: LeafletMarker }) => {
    const point = event.target.getLatLng()
    onPick({ latitude: point.lat, longitude: point.lng })
  }

  return (
    <div className="event-map-preview mt-3">
      <MapContainer
        center={[latitude, longitude]}
        zoom={17}
        scrollWheelZoom
        aria-label="Event location map"
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap contributors</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <RecenterMap latitude={latitude} longitude={longitude} />
        <MapInteraction disabled={disabled} onPick={onPick} />
        <Marker
          position={[latitude, longitude]}
          icon={pinIcon}
          draggable={!disabled}
          eventHandlers={{ dragend: handleDragEnd }}
        />
      </MapContainer>
      <small>Click the map or drag the pin to refine the exact entrance or venue. Map data © OpenStreetMap contributors.</small>
    </div>
  )
}
