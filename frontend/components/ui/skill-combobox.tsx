"use client"

import * as React from "react"
import { Check, ChevronsUpDown, X } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Badge } from "@/components/ui/badge"

interface SkillComboboxProps {
  availableSkills: string[]
  selectedSkills: string[]
  onSkillsChange: (skills: string[]) => void
  placeholder?: string
}

export function SkillCombobox({
  availableSkills,
  selectedSkills,
  onSkillsChange,
  placeholder = "Select or type skills...",
}: SkillComboboxProps) {
  const [open, setOpen] = React.useState(false)
  const [searchValue, setSearchValue] = React.useState("")

  const handleSelect = (skill: string) => {
    if (!selectedSkills.includes(skill)) {
      onSkillsChange([...selectedSkills, skill])
    }
    setSearchValue("")
    setOpen(false)
  }

  const handleRemove = (skill: string) => {
    onSkillsChange(selectedSkills.filter((s) => s !== skill))
  }

  const handleAddCustom = () => {
    const trimmedValue = searchValue.trim()
    if (trimmedValue && !selectedSkills.includes(trimmedValue)) {
      onSkillsChange([...selectedSkills, trimmedValue])
      setSearchValue("")
      setOpen(false)
    }
  }

  // Filter available skills based on search
  const filteredSkills = availableSkills.filter(
    (skill) => skill.toLowerCase().includes(searchValue.toLowerCase()) && !selectedSkills.includes(skill),
  )

  // Check if the search value is a new custom skill
  const isNewSkill =
    searchValue.trim() &&
    !availableSkills.some((skill) => skill.toLowerCase() === searchValue.toLowerCase()) &&
    !selectedSkills.some((skill) => skill.toLowerCase() === searchValue.toLowerCase())

  return (
    <div className="space-y-2">
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            aria-expanded={open}
            className="w-full justify-between bg-transparent"
          >
            <span className="truncate">{placeholder}</span>
            <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-full p-0" align="start">
          <Command shouldFilter={false}>
            <CommandInput placeholder="Type to search or add..." value={searchValue} onValueChange={setSearchValue} />
            <CommandList>
              <CommandEmpty>
                {isNewSkill ? (
                  <div className="p-2">
                    <Button variant="ghost" className="w-full justify-start" onClick={handleAddCustom}>
                      <span className="text-sm">Add "{searchValue}"</span>
                    </Button>
                  </div>
                ) : (
                  <div className="py-6 text-center text-sm">No skills found.</div>
                )}
              </CommandEmpty>
              {filteredSkills.length > 0 && (
                <CommandGroup>
                  {filteredSkills.map((skill) => (
                    <CommandItem key={skill} value={skill} onSelect={() => handleSelect(skill)}>
                      <Check
                        className={cn("mr-2 h-4 w-4", selectedSkills.includes(skill) ? "opacity-100" : "opacity-0")}
                      />
                      {skill}
                    </CommandItem>
                  ))}
                </CommandGroup>
              )}
              {isNewSkill && filteredSkills.length > 0 && (
                <CommandGroup heading="Add custom">
                  <CommandItem onSelect={handleAddCustom}>
                    <span className="text-sm">Add "{searchValue}"</span>
                  </CommandItem>
                </CommandGroup>
              )}
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>

      {selectedSkills.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {selectedSkills.map((skill) => (
            <Badge key={skill} variant="secondary" className="gap-1 pr-1">
              {skill}
              <button onClick={() => handleRemove(skill)} className="ml-1 rounded-full hover:bg-muted p-0.5">
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}
    </div>
  )
}
