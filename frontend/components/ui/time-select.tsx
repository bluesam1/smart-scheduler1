"use client"

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

interface TimeSelectProps {
  value?: string
  onChange?: (value: string) => void
  placeholder?: string
  className?: string
  id?: string
}

// Generate time options in 5-minute increments
function generateTimeOptions() {
  const options: string[] = []
  for (let hour = 0; hour < 24; hour++) {
    for (let minute = 0; minute < 60; minute += 5) {
      const timeValue = `${hour.toString().padStart(2, "0")}:${minute.toString().padStart(2, "0")}`
      options.push(timeValue)
    }
  }
  return options
}

// Format time for display (e.g., "08:00" -> "8:00 AM")
function formatTimeDisplay(time: string) {
  const [hours, minutes] = time.split(":").map(Number)
  const period = hours >= 12 ? "PM" : "AM"
  const displayHour = hours === 0 ? 12 : hours > 12 ? hours - 12 : hours
  return `${displayHour}:${minutes.toString().padStart(2, "0")} ${period}`
}

const TIME_OPTIONS = generateTimeOptions()

export function TimeSelect({ value, onChange, placeholder = "Select time", className, id }: TimeSelectProps) {
  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger id={id} className={className}>
        <SelectValue placeholder={placeholder}>{value ? formatTimeDisplay(value) : placeholder}</SelectValue>
      </SelectTrigger>
      <SelectContent className="max-h-[300px]">
        {TIME_OPTIONS.map((time) => (
          <SelectItem key={time} value={time}>
            {formatTimeDisplay(time)}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
