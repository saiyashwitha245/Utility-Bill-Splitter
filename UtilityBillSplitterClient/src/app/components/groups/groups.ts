import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, Group, GroupCreate, User } from '../../services/api';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-groups',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './groups.html',
  styleUrls: ['./groups.scss']
})
export class GroupsComponent implements OnInit {
  groups: Group[] = [];
  availableUsers: User[] = [];
  showCreateForm = false;
  showGroupDetails = false;
  showAddMember = false;
  showEditGroup = false;
  isLoading = false;
  isAddingMember = false;
  isUpdating = false;
  errorMessage = '';
  successMessage = '';
  selectedGroup: Group | null = null;
  
  groupForm: FormGroup;
  memberForm: FormGroup;
  editGroupForm: FormGroup;

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.groupForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['']
    });

    this.memberForm = this.fb.group({
      userId: ['', Validators.required]
    });

    this.editGroupForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      description: ['']
    });
  }

  ngOnInit() {
    this.loadGroups();
    this.loadAvailableUsers();
  }

  goBack() {
    this.router.navigate(['../']);
  }

  loadGroups() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.apiService.getGroups().subscribe({
      next: (groups) => {
        this.groups = groups;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading groups:', error);
        this.errorMessage = 'Failed to load groups. Please try again.';
        this.isLoading = false;
      }
    });
  }

  loadAvailableUsers() {
    this.apiService.getUsers().subscribe({
      next: (users) => {
        this.availableUsers = users;
      },
      error: (error) => {
        console.error('Error loading users:', error);
      }
    });
  }

  createGroup() {
    if (this.groupForm.valid && !this.isLoading) {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      const currentUser = this.authService.currentUserValue;
      if (!currentUser?.id) {
        this.errorMessage = 'User not authenticated';
        this.isLoading = false;
        return;
      }

      const groupData: GroupCreate = {
        name: this.groupForm.value.name,
        description: this.groupForm.value.description || '',
        createdById: currentUser.id,
        memberIds: [currentUser.id]
      };

      this.apiService.createGroup(groupData).subscribe({
        next: (newGroup) => {
          this.successMessage = 'Group created successfully!';
          this.showCreateForm = false;
          this.groupForm.reset();
          this.loadGroups();
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error creating group:', error);
          this.errorMessage = 'Failed to create group. Please try again.';
          this.isLoading = false;
        }
      });
    }
  }

  deleteGroup(group: Group) {
    if (confirm(`Are you sure you want to delete the group "${group.name}"? This action cannot be undone.`)) {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      this.apiService.deleteGroup(group.id).subscribe({
        next: () => {
          this.successMessage = `Group "${group.name}" deleted successfully!`;
          this.loadGroups();
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error deleting group:', error);
          this.errorMessage = 'Failed to delete group. Please try again.';
          this.isLoading = false;
        }
      });
    }
  }

  cancelCreate() {
    this.showCreateForm = false;
    this.groupForm.reset();
    this.errorMessage = '';
    this.successMessage = '';
  }

  viewGroup(group: Group) {
    this.selectedGroup = group;
    this.showGroupDetails = true;
  }

  showAddMemberForm(group: Group) {
    this.selectedGroup = group;
    this.memberForm.reset();
    this.showAddMember = true;
  }

  editGroup(group: Group) {
    this.selectedGroup = group;
    this.editGroupForm.patchValue({
      name: group.name,
      description: group.description
    });
    this.showEditGroup = true;
  }

  addMember() {
    if (this.memberForm.valid && this.selectedGroup) {
      this.isAddingMember = true;
      this.errorMessage = '';

      const userId = parseInt(this.memberForm.value.userId);
      const user = this.availableUsers.find(u => u.id === userId);
      
      if (user) {
        // Call backend API to add member
        this.apiService.addMemberToGroup(this.selectedGroup.id, userId).subscribe({
          next: (response) => {
            this.successMessage = `${user.username} added to group successfully!`;
            this.closeAddMemberModal();
            this.loadGroups(); // Reload to get updated member count
            this.isAddingMember = false;
          },
          error: (error) => {
            console.error('Error adding member:', error);
            this.errorMessage = error.error?.message || 'Failed to add member. Please try again.';
            this.isAddingMember = false;
          }
        });
      }
    }
  }

  updateGroup() {
    if (this.editGroupForm.valid && this.selectedGroup) {
      this.isUpdating = true;
      this.errorMessage = '';

      const updateData = {
        name: this.editGroupForm.value.name,
        description: this.editGroupForm.value.description
      };

      this.apiService.updateGroup(this.selectedGroup.id, updateData).subscribe({
        next: (response) => {
          this.successMessage = 'Group updated successfully!';
          this.closeEditModal();
          this.loadGroups();
          this.isUpdating = false;
        },
        error: (error) => {
          console.error('Error updating group:', error);
          this.errorMessage = 'Failed to update group. Please try again.';
          this.isUpdating = false;
        }
      });
    }
  }

  canEditGroup(group: Group): boolean {
    const currentUser = this.authService.currentUserValue;
    return currentUser?.id === group.createdById;
  }

  closeModal() {
    this.showGroupDetails = false;
    this.selectedGroup = null;
  }

  closeAddMemberModal() {
    this.showAddMember = false;
    this.selectedGroup = null;
    this.memberForm.reset();
    this.isAddingMember = false;
  }

  closeEditModal() {
    this.showEditGroup = false;
    this.selectedGroup = null;
    this.editGroupForm.reset();
    this.isUpdating = false;
  }
}
