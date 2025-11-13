"use client"

import * as React from "react"
import { X, ChevronDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

interface MultiSelectProps {
  options: string[]
  selected: string[]
  onChange: (selected: string[]) => void
  placeholder?: string
  disabled?: boolean
}

export function MultiSelect({
  options,
  selected,
  onChange,
  placeholder = "Select items...",
  disabled = false,
}: MultiSelectProps) {
  const [isOpen, setIsOpen] = React.useState(false)
  const [searchTerm, setSearchTerm] = React.useState("")
  const containerRef = React.useRef<HTMLDivElement>(null)

  // Close dropdown when clicking outside
  React.useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside)
      return () => document.removeEventListener("mousedown", handleClickOutside)
    }
  }, [isOpen])

  const handleToggle = (option: string) => {
    if (selected.includes(option)) {
      onChange(selected.filter((item) => item !== option))
    } else {
      onChange([...selected, option])
    }
  }

  const handleRemove = (option: string) => {
    onChange(selected.filter((item) => item !== option))
  }

  const handleAddCustom = () => {
    const trimmed = searchTerm.trim()
    if (trimmed && !selected.includes(trimmed) && !options.includes(trimmed)) {
      onChange([...selected, trimmed])
      setSearchTerm("")
    }
  }

  const filteredOptions = options.filter(
    (option) =>
      option.toLowerCase().includes(searchTerm.toLowerCase()) &&
      !selected.includes(option)
  )

  const canAddCustom =
    searchTerm.trim() &&
    !options.some((opt) => opt.toLowerCase() === searchTerm.toLowerCase()) &&
    !selected.some((sel) => sel.toLowerCase() === searchTerm.toLowerCase())

  return (
    <div ref={containerRef} className="relative space-y-2">
      <div className="relative">
        <Button
          type="button"
          variant="outline"
          disabled={disabled}
          onClick={() => !disabled && setIsOpen(!isOpen)}
          className={cn(
            "w-full justify-between font-normal",
            !selected.length && "text-muted-foreground"
          )}
        >
          <span className="truncate">
            {selected.length > 0
              ? `${selected.length} selected`
              : placeholder}
          </span>
          <ChevronDown className={cn("ml-2 h-4 w-4 shrink-0 transition-transform", isOpen && "rotate-180")} />
        </Button>

        {isOpen && (
          <div className="absolute z-[100] bottom-full mb-1 w-full rounded-md border bg-popover shadow-md">
            <div className="p-2 border-b">
              <Input
                type="text"
                placeholder="Search or type new skill..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && canAddCustom) {
                    e.preventDefault()
                    handleAddCustom()
                  }
                }}
                className="h-8"
              />
            </div>
            <div className="max-h-60 overflow-y-auto p-1">
              {filteredOptions.length === 0 && !canAddCustom && (
                <div className="py-6 text-center text-sm text-muted-foreground">
                  {searchTerm ? "No results found" : "No options available"}
                </div>
              )}
              {filteredOptions.map((option) => (
                <button
                  key={option}
                  type="button"
                  onClick={() => handleToggle(option)}
                  className="w-full text-left px-2 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground cursor-pointer"
                >
                  {option}
                </button>
              ))}
              {canAddCustom && (
                <button
                  type="button"
                  onClick={handleAddCustom}
                  className="w-full text-left px-2 py-1.5 text-sm rounded-sm hover:bg-accent hover:text-accent-foreground cursor-pointer border-t mt-1 pt-2"
                >
                  <span className="font-medium">Add:</span> "{searchTerm}"
                </button>
              )}
            </div>
          </div>
        )}
      </div>

      {selected.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {selected.map((item) => (
            <Badge key={item} variant="secondary" className="gap-1 pr-1">
              {item}
              <button
                type="button"
                onClick={() => handleRemove(item)}
                disabled={disabled}
                className="ml-1 rounded-full hover:bg-muted p-0.5 disabled:opacity-50"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}
    </div>
  )
}

